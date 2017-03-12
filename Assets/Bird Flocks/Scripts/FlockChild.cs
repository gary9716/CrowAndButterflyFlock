/**************************************									
	FlockChild v2.3
	Copyright Unluck Software	
 	www.chemicalbliss.com								
***************************************/

using UnityEngine;
using System;


public class FlockChild : MonoBehaviour
{

    bool _dived = true;
    [HideInInspector]
    public bool dived              //Indicates if this bird has recently performed a dive movement
    {
        set
        {
            _dived = value;
            if (animator != null)
            {
                animator.SetBool("dived", value);
            }
        }

        get
        {
            return _dived;
        }
    }


    bool _soar = true;
    [HideInInspector]
    public bool soar               // Indicates if this is soaring
    {
        set
        {
            _soar = value;
            if (animator != null)
            {
                animator.SetBool("soar", value);
            }
        }

        get
        {
            return _soar;
        }
    }



    bool _move = true;
    // Indicates if bird can fly
    [HideInInspector]
    public bool move
    {
        set
        {
            _move = value;
            if(animator != null)
            {
                animator.SetBool("moving", value);
            }
        }

        get
        {
            return _move;
        }
    }

    bool _idle;
    [HideInInspector]
    public bool idle
    {
        set
        {
            _idle = value;
            if(animator != null)
            {
                animator.SetBool("idle", value);
            }
        }

        get
        {
            return _idle;
        }
    }


    bool _landing;
    [HideInInspector]
    public bool landing
    {                   // Indicates if bird is landing or sitting idle
        set
        {
            _landing = value;
            if (animator != null)
            {
                animator.SetBool("landing", value);
            }
        }

        get
        {
            return _landing;
        }

    }
    [HideInInspector]
    public bool avoid = true;

    [HideInInspector]
    public FlockController _spawner;            //Reference to the flock controller that spawned this bird
    [HideInInspector]
    public Vector3 _wayPoint;               //Waypoint used to steer towards

    float _speed = 0;
    public float speed
    {                        //Current speed of bird
        set
        {
            _speed = value;
            if (animator != null)
                animator.SetFloat("moveSpeed", value);
        }

        get
        {
            return _speed;
        }

    }
    [HideInInspector]
    public float _stuckCounter;             //prevents looping around a waypoint by increasing minimum distance to waypoint
    [HideInInspector]
    public float _damping;                      //Damping used for steering (steer speed)

    int _lerpCounter;           // Used for smoothing motions like speed and leveling rotation
    [HideInInspector]
    public float _targetSpeed;                  // Max bird speed
                  
    public GameObject _model;                   // Reference to bird model
    public Transform _modelT;                   // Reference to bird model transform (caching tranform to avoid any extra getComponent calls)
    [HideInInspector]
    public float _avoidValue;                   //Random value used to check for obstacles. Randomized to lessen uniformed behaviour when avoiding
    [HideInInspector]
    public float _avoidDistance;                //How far from an obstacle this can be before starting to avoid it
    float _soarTimer;
    bool _instantiated;
    static int _updateNextSeed = 0;
    int _updateSeed = -1;
    public Transform _thisT;
    
    public Animator animator;

    public bool rotateAlignLandSpotWhenIdle;


    public void Start()
    {
        FindRequiredComponents();           //Check if references to transform and model are set (These should be set in the prefab to avoid doind this once a bird is spawned, click "Fill" button in prefab)
        Wander(0.0f);
        SetRandomScale();
        _thisT.position = findWaypoint();
        RandomizeStartAnimationFrame();
        InitAvoidanceValues();
        speed = _spawner._minSpeed;
        _spawner._activeChildren++;
        _instantiated = true;
        if (_spawner._updateDivisor > 1)
        {
            int _updateSeedCap = _spawner._updateDivisor - 1;
            _updateNextSeed++;
            this._updateSeed = _updateNextSeed;
            _updateNextSeed = _updateNextSeed % _updateSeedCap;
        }
    }

    public void Update()
    {
        //Skip frames
        if (_spawner._updateDivisor <= 1 || _spawner._updateCounter == _updateSeed)
        {
            SoarTimeLimit();
            CheckForDistanceToWaypoint();
            RotationBasedOnWaypointOrAvoidance();
            LimitRotationOfModel();
        }
    }

    public void OnDisable()
    {
        CancelInvoke();
        _spawner._activeChildren--;
    }

    public void OnEnable()
    {
        if (_instantiated)
        {
            _spawner._activeChildren++;

            if(animator == null)
            {
                if (landing)
                {
                    _model.GetComponent<Animation>().Play(_spawner._idleAnimation);
                }
                else {
                    _model.GetComponent<Animation>().Play(_spawner._flapAnimation);
                }
            }
            
        }
    }

    public void FindRequiredComponents()
    {
        if (_thisT == null) _thisT = transform;
        if (_model == null) _model = _thisT.FindChild("Model").gameObject;
        if (_modelT == null) _modelT = _model.transform;
    }

    public void RandomizeStartAnimationFrame()
    {
        if(animator == null)
            foreach (AnimationState state in _model.GetComponent<Animation>())
            {
                state.time = UnityEngine.Random.value * state.length;
            }
    }

    public void InitAvoidanceValues()
    {
        _avoidValue = UnityEngine.Random.Range(.3f, .1f);
        if (_spawner._birdAvoidDistanceMax != _spawner._birdAvoidDistanceMin)
        {
            _avoidDistance = UnityEngine.Random.Range(_spawner._birdAvoidDistanceMax, _spawner._birdAvoidDistanceMin);
            return;
        }
        _avoidDistance = _spawner._birdAvoidDistanceMin;
    }

    public void SetRandomScale()
    {
        float sc = UnityEngine.Random.Range(_spawner._minScale, _spawner._maxScale);
        _thisT.localScale = new Vector3(sc, sc, sc);
    }

    //Soar Timeout - Limits how long a bird can soar
    public void SoarTimeLimit()
    {
        if (this.soar && _spawner._soarMaxTime > 0)
        {
            if (_soarTimer > _spawner._soarMaxTime)
            {
                this.Flap();
                _soarTimer = 0.0f;
            }
            else {
                _soarTimer += _spawner._newDelta;
            }
        }
    }

    public void CheckForDistanceToWaypoint()
    {
        if (!landing && (_thisT.position - _wayPoint).magnitude < _spawner._waypointDistance + _stuckCounter)
        {
            Wander(0.0f);
            _stuckCounter = 0.0f;
        }
        else if (!landing)
        {
            _stuckCounter += _spawner._newDelta;
        }
        else {
            _stuckCounter = 0.0f;
        }
    }

    public void RotationBasedOnWaypointOrAvoidance()
    {
        Vector3 lookit = _wayPoint - _thisT.position;
        if (_targetSpeed > -1 && lookit != Vector3.zero)
        {
            Quaternion rotation = Quaternion.LookRotation(lookit);

            _thisT.rotation = Quaternion.Slerp(_thisT.rotation, rotation, _spawner._newDelta * _damping);
        }

        if (_spawner._childTriggerPos)
        {
            if ((_thisT.position - _spawner._posBuffer).magnitude < 1)
            {
                _spawner.SetFlockRandomPosition();
            }
        }
        speed = Mathf.Lerp(speed, _targetSpeed, _lerpCounter * _spawner._newDelta * .05f);
        _lerpCounter++;
        //Position forward based on object rotation
        if (move)
        {
            _thisT.position += _thisT.forward * speed * _spawner._newDelta;
            if (avoid && _spawner._birdAvoid)
                Avoidance();
        }
    }

    public bool Avoidance()
    {
        RaycastHit hit = new RaycastHit();
        Vector3 fwd = _modelT.forward;
        bool r = false;
        Quaternion rot = Quaternion.identity;
        Vector3 rotE = Vector3.zero;
        Vector3 pos = Vector3.zero;
        pos = _thisT.position;
        rot = _thisT.rotation;
        rotE = _thisT.rotation.eulerAngles;
        if (Physics.Raycast(_thisT.position, fwd + (_modelT.right * _avoidValue), out hit, _avoidDistance, _spawner._avoidanceMask))
        {
            rotE.y -= _spawner._birdAvoidHorizontalForce * _spawner._newDelta * _damping;
            rot.eulerAngles = rotE;
            _thisT.rotation = rot;
            r = true;
        }
        else if (Physics.Raycast(_thisT.position, fwd + (_modelT.right * -_avoidValue), out hit, _avoidDistance, _spawner._avoidanceMask))
        {
            rotE.y += _spawner._birdAvoidHorizontalForce * _spawner._newDelta * _damping;
            rot.eulerAngles = rotE;
            _thisT.rotation = rot;
            r = true;
        }
        if (_spawner._birdAvoidDown && !this.landing && Physics.Raycast(_thisT.position, -Vector3.up, out hit, _avoidDistance, _spawner._avoidanceMask))
        {
            rotE.x -= _spawner._birdAvoidVerticalForce * _spawner._newDelta * _damping;
            rot.eulerAngles = rotE;
            _thisT.rotation = rot;
            pos.y += _spawner._birdAvoidVerticalForce * _spawner._newDelta * .01f;
            _thisT.position = pos;
            r = true;
        }
        else if (_spawner._birdAvoidUp && !this.landing && Physics.Raycast(_thisT.position, Vector3.up, out hit, _avoidDistance, _spawner._avoidanceMask))
        {
            rotE.x += _spawner._birdAvoidVerticalForce * _spawner._newDelta * _damping;
            rot.eulerAngles = rotE;
            _thisT.rotation = rot;
            pos.y -= _spawner._birdAvoidVerticalForce * _spawner._newDelta * .01f;
            _thisT.position = pos;
            r = true;
        }
        return r;
    }

    public void LimitRotationOfModel()
    {
        Quaternion rot = Quaternion.identity;
        Vector3 rotE = Vector3.zero;
        rot = _modelT.localRotation;
        rotE = rot.eulerAngles;
        if ((soar && _spawner._flatSoar || _spawner._flatFly && !soar) && _wayPoint.y > _thisT.position.y || landing)
        {
            rotE.x = Mathf.LerpAngle(_modelT.localEulerAngles.x, -_thisT.localEulerAngles.x, _lerpCounter * _spawner._newDelta * .75f);
            rot.eulerAngles = rotE;
            _modelT.localRotation = rot;
        }
        else {
            rotE.x = Mathf.LerpAngle(_modelT.localEulerAngles.x, 0.0f, _lerpCounter * _spawner._newDelta * .75f);
            rot.eulerAngles = rotE;
            _modelT.localRotation = rot;
        }
    }

    public void Wander(float delay)
    {
        if (!landing)
        {
            _damping = UnityEngine.Random.Range(_spawner._minDamping, _spawner._maxDamping);
            _targetSpeed = UnityEngine.Random.Range(_spawner._minSpeed, _spawner._maxSpeed);
            _lerpCounter = 0;
            Invoke("SetRandomMode", delay);
        }
    }

    public void SetRandomMode()
    {
        CancelInvoke("SetRandomMode");
        if (!dived && UnityEngine.Random.value < _spawner._soarFrequency)
        {
            Soar();
        }
        else if (!dived && UnityEngine.Random.value < _spawner._diveFrequency)
        {
            Dive();
        }
        else {
            Flap();
        }
    }

    public void Flap()
    {
        if (move)
        {
            if (this._model != null && animator == null) _model.GetComponent<Animation>().CrossFade(_spawner._flapAnimation, .5f);
            soar = false;
            animationSpeed();
            _wayPoint = findWaypoint();
            dived = false;
        }
    }

    public void TakeOff()
    {
        avoid = true;
        //Reset flock child to flight mode
        _damping = _spawner._maxDamping;
        if(animator == null)
            _model.GetComponent<Animation>().CrossFade(_spawner._flapAnimation, .2f);
        dived = true;
        speed = 0.0f;
        move = true;
        landing = false;
        Flap();
        _wayPoint = new Vector3(_wayPoint.x, _thisT.position.y + 10, _wayPoint.z);
        
    }

    public void Landing(Transform landingSpotT, float distance, bool soarLand, float landingTurningSpeedModifier)
    {
        if (soarLand)
        {
            if(animator == null)
            {
                if (distance < 2)
                    _model.GetComponent<Animation>().CrossFade(_spawner._flapAnimation, .5f);
                else
                    _model.GetComponent<Animation>().CrossFade(_spawner._soarAnimation, .5f);
            }
            
        }
        _targetSpeed = _spawner._maxSpeed * .5f;
        _wayPoint = landingSpotT.position;
        _damping = landingTurningSpeedModifier;
        avoid = false;

    }

    public void FinalLand(Transform landingSpotT, float distance, float landingTurnSpeedModifier, float landingSpeedModifier, int lerpCounter)
    {
        _wayPoint = landingSpotT.position;

        if (distance > .01f)
        {
            _targetSpeed = _spawner._minSpeed * landingSpeedModifier;
            _thisT.position += (landingSpotT.position - _thisT.position) * Time.deltaTime * speed * landingSpeedModifier;
        }

        move = false;

        if(rotateAlignLandSpotWhenIdle)
        {
            
            Quaternion rot = _thisT.rotation;
            Vector3 rotE = rot.eulerAngles;
            rotE.y = Mathf.LerpAngle(_thisT.rotation.eulerAngles.y, landingSpotT.rotation.eulerAngles.y, lerpCounter * Time.deltaTime * .005f);
            rot.eulerAngles = rotE;
            _thisT.rotation = rot;
        
        }
        
        _damping = landingTurnSpeedModifier;
    }

    public Vector3 findWaypoint()
    {
        Vector3 t = Vector3.zero;
        t.x = UnityEngine.Random.Range(-_spawner._spawnSphere, _spawner._spawnSphere) + _spawner._posBuffer.x;
        t.z = UnityEngine.Random.Range(-_spawner._spawnSphereDepth, _spawner._spawnSphereDepth) + _spawner._posBuffer.z;
        t.y = UnityEngine.Random.Range(-_spawner._spawnSphereHeight, _spawner._spawnSphereHeight) + _spawner._posBuffer.y;
        return t;
    }

    public void Soar()
    {
        if (move)
        {
            if(animator == null)
                _model.GetComponent<Animation>().CrossFade(_spawner._soarAnimation, 1.5f);
            _wayPoint = findWaypoint();
            soar = true;
        }
    }

    public void Dive()
    {
        if (_spawner._soarAnimation != null && animator == null)
        {
            _model.GetComponent<Animation>().CrossFade(_spawner._soarAnimation, 1.5f);
        }
        else {
            if(animator == null)
                foreach (AnimationState state in _model.GetComponent<Animation>())
                {
                    if (_thisT.position.y < _wayPoint.y + 25)
                    {
                        state.speed = 0.1f;
                    }
                }
            else
            {
                if (_thisT.position.y < _wayPoint.y + 25)
                {
                    animator.speed = 0.1f / _spawner._maxAnimationSpeed;
                }
            }
        }
        _wayPoint = findWaypoint();
        _wayPoint.y -= _spawner._diveValue;
        dived = true;
    }

    public void animationSpeed()
    {
        if (animator == null)
            foreach (AnimationState state in _model.GetComponent<Animation>())
            {
                if (!dived && !landing)
                {
                    state.speed = UnityEngine.Random.Range(_spawner._minAnimationSpeed, _spawner._maxAnimationSpeed);
                }
                else {
                    state.speed = _spawner._maxAnimationSpeed;
                }
            }
        else
        {
            if (!dived && !landing)
                animator.speed = UnityEngine.Random.Range(_spawner._minAnimationSpeed, _spawner._maxAnimationSpeed) / _spawner._maxAnimationSpeed;
            else
                animator.speed = 1;
        }
    }
}
