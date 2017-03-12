/**************************************									
	Copyright Unluck Software	
 	www.chemicalbliss.com						
***************************************/

using UnityEngine;
using System;
using System.Collections;


public class LandingSpot : MonoBehaviour
{
    [HideInInspector]
    public FlockChild landingChild;

    bool _landing;
    [HideInInspector]
    public bool landing
    {
        set
        {
            _landing = value;
            if (landingChild != null)
            {
                landingChild.landing = _landing;
            }
        }

        get
        {
            return _landing;
        }
    }


    int lerpCounter;
    [HideInInspector]
    public LandingSpotController _controller;

    bool _idle;
    [HideInInspector]
    public bool idle
    {
        set
        {
            _idle = value;
            if (landingChild != null)
            {
                landingChild.idle = value;
            }
        }

        get
        {
            return _idle;
        }
    }

    public Transform _thisT;                    //Reference to transform component


    public void Start()
    {
        if (_thisT == null) _thisT = transform;
        if (_controller == null)
            _controller = _thisT.parent.GetComponent<LandingSpotController>();
        if (_controller._autoCatchDelay.x > 0)
            StartCoroutine(GetFlockChild(_controller._autoCatchDelay.x, _controller._autoCatchDelay.y));
        RandomRotate();
    }

    public void OnDrawGizmos()
    {
        if (_thisT == null) _thisT = transform;
        if (_controller == null)
            _controller = _thisT.parent.GetComponent<LandingSpotController>();

        Gizmos.color = Color.yellow;
        // Draw a yellow cube at the transforms position
        if ((landingChild != null) && landing)
            Gizmos.DrawLine(_thisT.position, landingChild._thisT.position);
        if (_thisT.rotation.eulerAngles.x != 0 || _thisT.rotation.eulerAngles.z != 0)
            _thisT.eulerAngles = new Vector3(0.0f, _thisT.eulerAngles.y, 0.0f);
        Gizmos.DrawWireCube(new Vector3(_thisT.position.x, _thisT.position.y, _thisT.position.z), new Vector3(.2f, .2f, .2f));
        Gizmos.DrawWireCube(_thisT.position + (_thisT.forward * .2f), new Vector3(.1f, .1f, .1f));
        Gizmos.color = new Color(1.0f, 1.0f, 0.0f, .05f);
        Gizmos.DrawWireSphere(_thisT.position, _controller._maxBirdDistance);
    }

    public void LateUpdate()
    {
        if (_controller._flock.gameObject.activeInHierarchy && landing && (landingChild != null))
        {
            if (!landingChild.gameObject.activeInHierarchy)
            {
                StartCoroutine(ReleaseFlockChild(0.0f, 0.0f));
            }
            //Check distance to flock child
            float distance = Vector3.Distance(landingChild._thisT.position, _thisT.position);
            //Start landing if distance is close enough

            if (distance < 5 && distance > .5f)
            {
                landingChild.Landing(_thisT, distance, _controller._soarLand, _controller._landingTurnSpeedModifier);
            }
            else if (distance <= .5f)
            {
                if (distance < .1f && !idle)
                {
                    idle = true;
                    if (landingChild.animator == null)
                        landingChild._model.GetComponent<Animation>().CrossFade(landingChild._spawner._idleAnimation, .55f);
                }

                lerpCounter++;

                landingChild.FinalLand(_thisT, distance, _controller._landingTurnSpeedModifier, _controller._landingSpeedModifier, lerpCounter);

            }
            else {
                //Move towards landing spot
                landingChild._wayPoint = _thisT.position;
                landingChild._damping = 1.0f;
            }
        }
    }

    public IEnumerator GetFlockChild(float minDelay, float maxDelay)
    {
        yield return new WaitForSeconds(UnityEngine.Random.Range(minDelay, maxDelay));
        if (_controller._flock.gameObject.activeInHierarchy && (landingChild == null))
        {
            RandomRotate();

            FlockChild fChild = null;

            for (int i = 0; i < _controller._flock._roamers.Count; i++)
            {
                FlockChild child = _controller._flock._roamers[i];
                if (!child.landing && !child.dived)
                {
                    if (!_controller._onlyBirdsAbove)
                    {
                        if ((fChild == null) && _controller._maxBirdDistance > Vector3.Distance(child._thisT.position, _thisT.position) && _controller._minBirdDistance < Vector3.Distance(child._thisT.position, _thisT.position))
                        {
                            fChild = child;
                            if (!_controller._takeClosest) break;
                        }
                        else if ((fChild != null) && Vector3.Distance(fChild._thisT.position, _thisT.position) > Vector3.Distance(child._thisT.position, _thisT.position))
                        {
                            fChild = child;
                        }
                    }
                    else {
                        if ((fChild == null) && child._thisT.position.y > _thisT.position.y && _controller._maxBirdDistance > Vector3.Distance(child._thisT.position, _thisT.position) && _controller._minBirdDistance < Vector3.Distance(child._thisT.position, _thisT.position))
                        {
                            fChild = child;
                            if (!_controller._takeClosest) break;
                        }
                        else if ((fChild != null) && child._thisT.position.y > _thisT.position.y && Vector3.Distance(fChild._thisT.position, _thisT.position) > Vector3.Distance(child._thisT.position, _thisT.position))
                        {
                            fChild = child;
                        }
                    }
                }
            }
            if (fChild != null)
            {
                landingChild = fChild;
                landing = true;
                StartCoroutine(ReleaseFlockChild(_controller._autoDismountDelay.x, _controller._autoDismountDelay.y));
            }
            else if (_controller._autoCatchDelay.x > 0)
            {
                StartCoroutine(GetFlockChild(_controller._autoCatchDelay.x, _controller._autoCatchDelay.y));
            }
        }
    }

    public void RandomRotate()
    {
        if (_controller._randomRotate)
        {
            Quaternion rot = _thisT.rotation;
            Vector3 rotE = rot.eulerAngles;
            rotE.y = (float)UnityEngine.Random.Range(0, 360);
            rot.eulerAngles = rotE;
            _thisT.rotation = rot;
        }
    }

    public void InstantLand()
    {
        if (_controller._flock.gameObject.activeInHierarchy && (landingChild == null))
        {
            FlockChild fChild = null;

            for (int i = 0; i < _controller._flock._roamers.Count; i++)
            {
                FlockChild child = _controller._flock._roamers[i];
                if (!child.landing && !child.dived)
                {
                    fChild = child;
                }
            }
            if (fChild != null)
            {
                landingChild = fChild;
                landing = true;
                landingChild._thisT.position = _thisT.position;
                if(landingChild.animator == null)
                    landingChild._model.GetComponent<Animation>().Play(landingChild._spawner._idleAnimation);

                StartCoroutine(ReleaseFlockChild(_controller._autoDismountDelay.x, _controller._autoDismountDelay.y));
            }
            else if (_controller._autoCatchDelay.x > 0)
            {
                StartCoroutine(GetFlockChild(_controller._autoCatchDelay.x, _controller._autoCatchDelay.y));
            }
        }
    }

    public IEnumerator ReleaseFlockChild(float minDelay, float maxDelay)
    {
        yield return new WaitForSeconds(UnityEngine.Random.Range(minDelay, maxDelay));
        if (_controller._flock.gameObject.activeInHierarchy && (landingChild != null))
        {
            lerpCounter = 0;
            if (_controller._featherPS != null)
            {
                _controller._featherPS.position = landingChild._thisT.position;
                _controller._featherPS.GetComponent<ParticleSystem>().Emit(UnityEngine.Random.Range(0, 3));
            }
            landing = false;
            idle = false;
            landingChild.TakeOff();
            yield return new WaitForSeconds(.1f);
            if (_controller._autoCatchDelay.x > 0)
            {
                StartCoroutine(GetFlockChild(_controller._autoCatchDelay.x, _controller._autoCatchDelay.y));
            }
            landingChild = null;
        }
    }
}
