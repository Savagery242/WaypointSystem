using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public delegate void NodeReachedEvent();

namespace Waypoints
{
    public class WPNode : MonoBehaviour
    {

        //==================================================
        //  SERIALIZED FOR INSPECTOR
        //==================================================

        [Range(0.0f, 2.0f)] public float        shape;
        public NODE_GROUP   nodeGroup;
        public TRAVERSAL    traversal;
        public bool         changeDirection;
        public SPEED_CURVE  speedCurve;
        public float        speed;
        public float        time;        

        //==================================================
        //  PUBLIC
        //==================================================

        public event    NodeReachedEvent nodeReached;

        public int      nodeNumber { get; set; }
        public bool     bypassed { get; set; }        
        
        WPController _controller;
        public WPController controller
        { get { return _controller ?? (_controller = GetComponentInParent<WPController>()); } }
    }
}

