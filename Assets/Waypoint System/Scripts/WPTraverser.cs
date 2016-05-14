using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Waypoints
{
    public class WPTraverser : MonoBehaviour
    {
        //==================================================
        //  PUBLIC
        //==================================================

        public bool             debugMode;
        public WPController[]   controllers;
        public int              defaultController;
        public bool             useRigidbody = true;
        public LOOP             runInLoop;
        public float            moveSpeed = 1.0f;

        //==================================================
        //  PRIVATE
        //==================================================

        WPController    controller;
        Rigidbody       rb;        
        bool            reverse;        
        int             curSub;
        bool            traversing;
  
        WPSegment _curSeg;
        WPSegment curSeg
        {
            get { return _curSeg; }
            set
            {
                if (value != _curSeg)
                {
                    // If you are switching segments, remove this gameobject from the inhabitor
                    // list of the segment, and add it to the new one.
                    if (_curSeg != null) _curSeg.RemoveInhabitor(this.gameObject);
                    _curSeg = value;
                    if (_curSeg != null) _curSeg.AddInhabitor(this.gameObject);
                }
            }
        }

        //==================================================
        //  PRIVATE METHODS
        //==================================================        

        void TraverseFromFirstNode()
        {
            curSeg = controller.GetFirstSeg();
            curSub = 0;
            traversing = true;
        }

        void TraverseFromClosestPoint()
        {
            WPSegment seg;
            int sub;
            controller.GetNearestSegAndSub(transform.position, out seg, out sub);
            curSeg = seg;
            curSub = sub;
            traversing = true;
        }

        void ChangePath(WPController newController)
        {
            if (newController == null || newController == controller) return;
            controller = newController;
            TraverseFromClosestPoint();
        }

        //--------------------------------------------------
        //  Main traversal loop
        //--------------------------------------------------

        void TraverseLoop()
        {
            if (curSeg == null) return;

            int lastSubNode  = curSeg.subNodes.Length - 1;
            Vector3 curPos   = (useRigidbody) ? rb.position : transform.position;
           
            bool willOvershoot = false;

            // Prevent unforseen endless loop
            float failsafe = 0.0f; 
            const float failsafeTime = 0.5f;

            // if movement method returns that speed will overshoot,
            // keep running through segments until it no longer will.

            do
            {
                Vector3 dest = controller.transform.position + curSeg.subNodes[curSub];
                willOvershoot = MoveBySpeed(dest, moveSpeed, ref curPos);

                if (willOvershoot)
                {
                    curSub += (reverse) ? -1 : 1;
                }

                if (curSub < 0 || curSub > lastSubNode)
                {
                    WPNode curNode = (!reverse) ? curSeg.nodeB : curSeg.nodeA;
                    if (curNode.changeDirection)
                    {
                        reverse = !reverse; // if changing direction, no need to find new seg, just reset subnode
                    }
                    else
                    {
                        curSeg = controller.GetNextSeg(curSeg, reverse);
                    }
                    if (curSeg == null)
                    {
                        Debug.Log("Traversal complete: " + gameObject.name);
                        traversing = false;
                        return;
                    }
                    curSub = (reverse) ? lastSubNode - 1 : 1;
                }
                failsafe += Time.deltaTime;
            } while (willOvershoot && failsafe < failsafeTime);                 

            if (!willOvershoot)
            {
                if (useRigidbody)
                {
                    rb.MovePosition(curPos);
                }
                else
                {
                    transform.position = curPos;
                }
            }
        }

        //--------------------------------------------------
        // All movement methods return true if subnode is
        // reached.
        //--------------------------------------------------

        bool MoveBySpeed(Vector3 destination, float speed, ref Vector3 curPos)
        {
            bool willOvershoot = false;
            speed *= Time.deltaTime;
            Vector3 heading = destination - curPos;
            float distRemaining = heading.sqrMagnitude;
            if (distRemaining < (speed * speed))
            {
                willOvershoot = true;
            }
            else
            {
                heading.Normalize();
                heading *= speed;
                curPos += heading;
            }
            return willOvershoot;
        }

        //--------------------------------------------------
        //  Debug Input
        //--------------------------------------------------

        void GetDebugInput()
        {
            if (Input.GetKeyDown(KeyCode.T) && !traversing)     TraverseFromClosestPoint();
            if (Input.GetKeyDown(KeyCode.Alpha0) && traversing) ChangePath(controllers[0]);
            if (Input.GetKeyDown(KeyCode.Alpha1) && traversing) ChangePath(controllers[1]);
        }

        //==================================================
        //  UNITY METHODS
        //==================================================

        void Start()
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                useRigidbody = false;
            }
            if (controllers == null || controllers.Length == 0)
            {
                Debug.LogError("No Controller set for WPTraverser attached to " + name);
            }
            defaultController = Mathf.Clamp(defaultController, 0, controllers.Length - 1);
            controller = controllers[defaultController];
        }

        void Update()
        {
            if (traversing && runInLoop == LOOP.UPDATE) TraverseLoop();
            if (debugMode) GetDebugInput();       
        }
        void FixedUpdate()
        {
            if (traversing && runInLoop == LOOP.FIXEDUPDATE) TraverseLoop();
        }
        void LateUpdate()
        {
            if (traversing && runInLoop == LOOP.LATEUPDATE) TraverseLoop();
        }
        public enum LOOP
        {
            UPDATE,
            FIXEDUPDATE,
            LATEUPDATE
        };
    }
}
