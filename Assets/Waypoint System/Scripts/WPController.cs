using UnityEngine;
using System.Collections.Generic;

namespace Waypoints
{
    public class WPController : MonoBehaviour
    {
        //==================================================
        //  SERIALIZED FOR INSPECTOR
        //==================================================

        [SerializeField][Range(5, 50)] int pathResolution = 20;
        [SerializeField] bool isLooping;

        [SerializeField] Color group1Color = Color.magenta;
        [SerializeField] Color group2Color = Color.cyan;
        [SerializeField] Color group3Color = Color.blue;

        //==================================================
        //  PRIVATE
        //==================================================

        List<WPNode>    nodes    = new List<WPNode>(); // All nodes 
        List<WPSegment> segments = new List<WPSegment>(); // Segments
        SegmentPool segPool      = new SegmentPool(8); // Segment pool to allow segment object recycling

        int _resolution;
        int resolution
        {
            get { return (Application.isPlaying) ? _resolution : pathResolution; }
            set { _resolution = value; }
        }

        //==================================================
        //  PUBLIC METHODS
        //==================================================

        public void ToggleGroup(NODE_GROUP g, bool state)
        {
            foreach (var s in segments)
            {
                if(!s.isInhabited())
                {
                    if (s.nodeA.nodeGroup == g) s.nodeA.bypassed = !state;
                    if (s.nodeB.nodeGroup == g) s.nodeB.bypassed = !state;
                }
            }
            BuildPathFast();
        }

        public void ToggleGroup(NODE_GROUP g)
        {
            List<WPNode> toToggle = nodes.FindAll((n) => n.nodeGroup == g);
            foreach (var s in segments)
            {
                if (s.isInhabited())
                {
                    if (toToggle.Contains(s.nodeA)) toToggle.Remove(s.nodeA);
                    if (toToggle.Contains(s.nodeB)) toToggle.Remove(s.nodeB);                    
                }
            }
            toToggle.ForEach((n) => n.bypassed = !n.bypassed);
            BuildPathFast();
        }

        //==================================================
        //  Finding Next Segment
        //==================================================

        public WPSegment GetFirstSeg()
        {
            return segments[0];
        }

        //--------------------------------------------------
        // Forward motion means moving from node A to node B,
        // B to A for backwards motion. Next segment is found 
        // by matching the end node to the beginning node of
        // another segment
        //--------------------------------------------------

        public WPSegment GetNextSeg(WPSegment curSeg, bool reverse)
        {
            WPNode curNode = (!reverse) ? curSeg.nodeB : curSeg.nodeA; // Cache the end node of the first segment
            WPSegment nextSeg = null;

            foreach (var s in segments)
            {
                if (!reverse)
                {
                    if (s.nodeA == curNode)
                    {
                        nextSeg = s;
                        break;
                    }
                }
                else
                {
                    if (s.nodeB == curNode)
                    {
                        nextSeg = s;
                        break;
                    }
                }
            }
            if (nextSeg == null)
            {                 
                Debug.Log("Next segment not found. Path is complete or destroyed.");
            }
            return nextSeg;
        }

        public void GetNearestSegAndSub(Vector3 traverser, out WPSegment seg, out int sub)
        {
            float nearestSqDist = float.MaxValue;
            seg = null;
            sub = 0;
            foreach (var s in segments)
            {
                for (int i = 0; i < s.subNodes.Length; ++i)
                {
                    float sqDistFromSub = (s.subNodes[i] - traverser).sqrMagnitude;
                    if (sqDistFromSub < nearestSqDist)
                    {                        
                        seg = s;
                        sub = i;
                        nearestSqDist = sqDistFromSub;
                    }
                }
            }
            if (seg == null)
            {
                seg = GetFirstSeg();
            }                        
        }

        //==================================================
        //  PRIVATE METHODS
        //==================================================

        //--------------------------------------------------
        // Populates the segment list with end node objects
        // and the subnode list, according to the desired
        // resolution. All positions in a segment are in
        // local space relative to the controller object.
        //--------------------------------------------------

        void BuildSegmentList(List<WPNode> activeList)
        {

            WPNode[] nodes = activeList.ToArray();
            segments.Clear();
            segPool.Clean();

            if (nodes.Length < 2)
            {
                Debug.LogError("Active Node List is of invalid size (less than 2 WPNodes)");
                return;
            }

            int length = (isLooping) ? nodes.Length : nodes.Length - 1;  // Do not build segment for the last node if not looping

            Vector3 mp1 = Vector3.zero;
            Vector3 mp2 = Vector3.zero;
            Vector3 mp3 = Vector3.zero;
            Vector3 e1 = Vector3.zero;
            Vector3 e2 = Vector3.zero;

            for (int index = 0; index < length; index++)
            {

                /*--------------------------------------------
                Find 4 points for cubic bezier describing 3 
                segments, duplicating first and last if at 
                ends.
                --------------------------------------------*/

                int indexMinus1 = index - 1;
                int indexPlus1 = index + 1;
                int indexPlus2 = index + 2;

                if (isLooping)
                {
                    indexMinus1 = (indexMinus1 < 0) ? nodes.Length - 1 : 0;
                    indexPlus1 = indexPlus1 % nodes.Length;
                    indexPlus2 = indexPlus2 % nodes.Length;                
                }

                indexMinus1 = Mathf.Clamp(indexMinus1, 0, nodes.Length - 1);
                indexPlus1 = Mathf.Clamp(indexPlus1, 0, nodes.Length - 1);
                indexPlus2 = Mathf.Clamp(indexPlus2, 0, nodes.Length - 1);

                Vector3 p1 = nodes[indexMinus1].transform.localPosition;
                Vector3 p2 = nodes[index].transform.localPosition;
                Vector3 p3 = nodes[indexPlus1].transform.localPosition;
                Vector3 p4 = nodes[indexPlus2].transform.localPosition;

                /*--------------------------------------------
                Find midpoints of 3 segments. Wrapped in an 
                if statement, since there is no reason to
                recalculate the values; I am reusing 2 out of
                3 of the values the next iteration.
                --------------------------------------------*/               

                if (index == 0)
                {
                    mp1 = Vector3.Lerp(p1, p2, 0.5f);
                    mp2 = Vector3.Lerp(p2, p3, 0.5f);
                }
                else
                {
                    mp1 = mp2;
                    mp2 = mp3;
                }

                mp3 = Vector3.Lerp(p3, p4, 0.5f);

                /*--------------------------------------------
                Find point along midpoint connecting vectors, 
                using relative scale of sides. Again, if this
                is the first iteration, calculate values,
                otherwise just reuse.
                --------------------------------------------*/

                if (index == 0)
                {
                    e1 = Vector3.Lerp(mp1, mp2, Vector3.Distance(p1, p2) / (Vector3.Distance(p1, p2) + Vector3.Distance(p2, p3)));
                }
                else
                {
                    e1 = e2;
                }

                float dist = Vector3.Distance(p2, p3); // To avoid doing distance operation twice next line
                e2 = Vector3.Lerp(mp2, mp3, dist / (dist + Vector3.Distance(p3, p4)));

                /*--------------------------------------------
                Calculate control points of bezier curve.
                s1 and s4 are commented out because, while I 
                could store the values as above, it's really 
                not worth if for a simple operation.

                //Vector3 s1 = p2 + (mp1 - e1);
                //Vector3 s4 = p3 + (mp3 - e2);
                --------------------------------------------*/

                Vector3 s2 = p2 + ((mp2 - e1) * nodes[index].shape);
                Vector3 s3 = p3 + ((mp2 - e2) * nodes[indexPlus1].shape);

                /*--------------------------------------------
                Iterate through and calculate all the points!
                --------------------------------------------*/

                float x, y, z;

                Vector3[] subnodes       = new Vector3[resolution + 2]; // +2 for the start and end nodes
                subnodes[0]              = nodes[index].transform.localPosition; // Start node
                subnodes[resolution + 1] = nodes[indexPlus1].transform.localPosition; // End node

                //--------------------------------------------------
                //  Find intermediate points
                //--------------------------------------------------

                for (int k = 0; k < resolution; k++)
                {
                    // t cannot be 0 or 1, or else the subnode ends up on the real node.
                    // So, in order to make all subnodes between real nodes, we are faking a higher
                    // resolution.

                    float t = (float)(k+1) / (resolution + 1); 
                    
                    // Alas, I don't understand this next formula. Courtesy of http://pomax.github.io/bezierinfo/ 
                    // These are the equations for actually calculating the Bezier points.

                    x = p2.x * (1 - t).Pow(3) + s2.x * 3 * (1 - t).Pow(2) * t + s3.x * 3 * (1 - t) * t.Pow(2) + p3.x * t.Pow(3);
                    y = p2.y * (1 - t).Pow(3) + s2.y * 3 * (1 - t).Pow(2) * t + s3.y * 3 * (1 - t) * t.Pow(2) + p3.y * t.Pow(3);
                    z = p2.z * (1 - t).Pow(3) + s2.z * 3 * (1 - t).Pow(2) * t + s3.z * 3 * (1 - t) * t.Pow(2) + p3.z * t.Pow(3);

                    // If we have reached the destination node, assign this node to our Subnode
                    // start at index k + 1 to leave index 0 and (length - 1) alone, which are our end node positions.
                    subnodes[k + 1] = new Vector3(x, y, z);
                }

                WPSegment newSeg = segPool.GetSeg(nodes[index], nodes[indexPlus1], subnodes);
                segments.Add(newSeg);      
            }
        }

        //--------------------------------------------------
        // Short circuit this method while game is playing
        // to avoid unnecessary runtime allocation. During
        // runtime, it's not allowed to reorganize, remove,
        // or rename nodes, only to bypass them in groups.
        //--------------------------------------------------
                
        bool BuildPathFast()
        {            
            List<WPNode> activeNodes = nodes.FindAll((n) => n != null && !n.bypassed && n.enabled && n.gameObject.activeSelf);
            if (activeNodes.Count < 2)
            {
                Debug.LogWarning("Fewer than 2 active nodes in path " + gameObject.name + ". Path creation failed");
                return false;
            }
            else
            {
                activeNodes.Sort((x, y) => x.nodeNumber.CompareTo(y.nodeNumber));
                BuildSegmentList(activeNodes);
                return true;
            }
        }

        //--------------------------------------------------
        //  If the application is not running, you can
        // reorder and remove nodes and the path will be
        // updated. This creates some overhead that I wanted
        // to avoid by using the code block above.
        //--------------------------------------------------

        bool BuildPathFull()
        {
            nodes.Clear();
            nodes.AddRange(GetComponentsInChildren<WPNode>());

            // List validity checking

            List<WPNode> activeNodes = nodes.FindAll((n) => n != null && !n.bypassed && n.enabled && n.gameObject.activeSelf);

            if (activeNodes.Count < 2)
            {
                Debug.LogError("Path needs at least two active and enabled nodes!");
                return false;
            }            

            if (HierarchyChanged())
            {
                foreach (var n in nodes)
                {
                    n.nodeNumber = n.transform.GetSiblingIndex();

                    if (n.nodeNumber == 0) {
                        n.gameObject.name = "Node: 0 (START)";
                    } else if (n.nodeNumber == nodes.Count - 1) {
                        n.gameObject.name = "Node: " + n.nodeNumber + " (END)";
                    } else {
                        n.gameObject.name = "Node: " + n.nodeNumber;
                    }
                }
            }

            // Sort nodes in order of node number, determined by their siblingIndex

            nodes.Sort((x, y) => x.nodeNumber.CompareTo(y.nodeNumber)); 
            activeNodes.Sort((x, y) => x.nodeNumber.CompareTo(y.nodeNumber));

            // Generate the Bezier curves and populate each node's subNode Lists            
            
            BuildSegmentList(activeNodes);
            return true;
        }

        /*--------------------------------------------
        Check if the order of the path has been changed
        in the hierarchy, or if another node has been
        added.
        --------------------------------------------*/

        bool HierarchyChanged()
        {
            bool changed = false;
            WPNode[] nodes = gameObject.GetComponentsInChildren<WPNode>();

            // Has the number of nodes changed?

            if (nodes.Length != this.nodes.Count) changed = true;

            // Has the order of the nodes changed?

            foreach (var n in nodes)
            {
                if (n.transform.GetSiblingIndex() != n.nodeNumber) changed = true;
            }
            return changed;
        }

        //==================================================
        //  UNITY METHODS
        //==================================================

        void Awake()
        {
            resolution = pathResolution;
            BuildPathFull();
        }

        //==================================================
        //  GIZMO DRAWING
        //==================================================

        void OnDrawGizmos()
        {

            if (!Application.isPlaying)
            {
                if (!BuildPathFull())
                {
                    Debug.LogError("Invalid path!");
                    return;
                }
            }

            // Draw the Gizmos.
            // Red circle = START
            // Green circle = END
            // White line = path is clear
            // If just one node, make a yellow sphere

            List<WPNode> activeList = new List<WPNode>();
            nodes.ForEach((x) => { if (!x.bypassed) activeList.Add(x); });

            for (int i = 0; i < activeList.Count; i++ )
            {

                Color groupColor = Color.white;

                switch (activeList[i].nodeGroup)
                {
                    default:
                    case NODE_GROUP.UNGROUPED:
                        groupColor = Color.white;
                        break;
                    case NODE_GROUP.GROUP1:
                        groupColor = group1Color;
                        break;
                    case NODE_GROUP.GROUP2:
                        groupColor = group2Color;
                        break;
                    case NODE_GROUP.GROUP3:
                        groupColor = group3Color;
                        break;
                }

                if (!isLooping)
                {
                    if (i == 0) {
                        Gizmos.color = Color.red;
                        Gizmos.DrawWireSphere(activeList[i].transform.position, 1.0f);
                    } else if (i == activeList.Count - 1) {
                        Gizmos.color = Color.green;
                        Gizmos.DrawWireSphere(activeList[i].transform.position, 1.0f);
                    } else {
                        Gizmos.color = groupColor;
                        Gizmos.DrawWireSphere(activeList[i].transform.position, 0.5f);
                    } 
                }
                else
                {
                    if (i == 0) {
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawWireSphere(activeList[i].transform.position, 1.0f);
                    } else {
                        Gizmos.color = groupColor;
                        Gizmos.DrawWireSphere(activeList[i].transform.position, 0.5f);
                    }
                }                           
            }

            foreach (var s in segments)
            {
                Gizmos.color = Color.white;
                for (int i = 0; i < s.subNodes.Length - 1; i++ )
                {
                    Gizmos.DrawLine(transform.position + s.subNodes[i], transform.position + s.subNodes[i + 1]);
                }
            }
        }

    } // Class

    //==================================================
    //  ENUMS
    //==================================================

    public enum TRAVERSAL
    {
        SPEED,
        TIME
    };

    public enum SPEED_CURVE
    {
        JUMP,
        LINEAR,
        EXPONENTIAL,
    };

    public enum NODE_GROUP
    {
        UNGROUPED,
        GROUP1,
        GROUP2,
        GROUP3
    };

    //==================================================
    // HELPER TYPES
    //==================================================

    //--------------------------------------------------
    // The segment pool is used to recycle path segments.
    // With many paths possible per game, and them being
    // rebuilt due to disabling groups/nodes, we would
    // end up with a bunch of dead objects that just add
    // to the load of the shitty Mono 2.x GC. I'm pooling
    // to avoid that even though it makes life harder...
    //--------------------------------------------------

    public class SegmentPool
    {
        List<WPSegment> pool;

        public SegmentPool(int numSegs)
        {
            pool = new List<WPSegment>(numSegs);
        }

        //--------------------------------------------------
        // Get a new segment from the pool.
        //--------------------------------------------------

        public WPSegment GetSeg(WPNode nodeA, WPNode nodeB, Vector3[] subNodes)
        {
            WPSegment returnVal = null;           
            
            for (int i = 0; i < pool.Count; ++i)
            {
                // If a segment with the same start and end node is found in the pool,
                // return that. No need to make a new object.
                if (pool[i].nodeA == nodeA && pool[i].nodeB == nodeB)
                {
                    returnVal = pool[i];
                    break;
                }
                // Otherwise, check to see if there is another pool object that's ready
                // to be recycled (dead) in the pool. If so, use that.
                if (pool[i].isDead)
                {
                    returnVal = pool[i];
                }
            }           
            if (returnVal == null)  // Only if there are no matching or dead segments, create a new object.
            {
                returnVal = new WPSegment(nodeA, nodeB, subNodes);
                //Debug.Log("new segment created");
                pool.Add(returnVal);
            }
            else // Otherwise populate the recycled object with the desired values.
            {
                returnVal.nodeA    = nodeA;
                returnVal.nodeB    = nodeB;
                returnVal.subNodes = subNodes;
            }
            return returnVal;
        }
        public void Clean()
        {
            for (int i = 0; i < pool.Count; ++i)
            {
                if (!pool[i].isInhabited()) pool[i].Initialize();
            }
        }
        public void Clear()
        {
            pool.Clear();
        }
    }

    public class WPSegment
    {
        public WPNode     nodeA      { get; set; }
        public WPNode     nodeB      { get; set; }
        public Vector3[]  subNodes   { get; set; }

        List<GameObject> inhabitors = new List<GameObject>();

        public bool isDead
        {
            get
            {
                if (isInhabited())
                {
                    return false;
                }
                else
                {
                    return (nodeA    == null ||
                            nodeB    == null ||
                            subNodes == null ||
                            (nodeA.bypassed  || nodeB.bypassed));
                }
            }
        }
        public bool isInhabited()
        {
            bool inhabited = false;
            for (int i = 0; i < inhabitors.Count; ++i)
            {
                if (inhabitors[i] != null && inhabitors[i].activeSelf)
                {
                    inhabited = true;
                }
            }
            return inhabited;
        }
        public void AddInhabitor(GameObject go)
        {
            if (!inhabitors.Contains(go)) inhabitors.Add(go);
        }
        public void RemoveInhabitor(GameObject go)
        {
            if (inhabitors.Contains(go)) inhabitors.Remove(go);
        }
        public WPSegment()
        {
            Initialize();
        }
        public WPSegment(WPNode nodeA, WPNode nodeB, Vector3[] subNodes)
        {
            this.nodeA    = nodeA;
            this.nodeB    = nodeB;
            this.subNodes = subNodes;
        }                
        public void Initialize()
        {
            nodeA       = null;
            nodeB       = null;
            subNodes    = null;
            inhabitors  = new List<GameObject>(); ;
        }
        public static bool operator == (WPSegment A, WPSegment B)
        {
            if (ReferenceEquals(A, B))
            {
                return true;
            }
            if (((object)A == null) || ((object)B == null))
            {
                return false;
            }
            return (A.nodeA.gameObject == B.nodeA.gameObject && A.nodeB.gameObject == B.nodeB.gameObject);
        }
        public static bool operator != (WPSegment A, WPSegment B)
        {
            return (!(A == B));
        }
        public override bool Equals(object obj)
        {
            return ((object)this == obj);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public override string ToString()
        {
            if (nodeA == null || nodeB == null) return base.ToString();
            else return "NodeA: " + nodeA.nodeNumber + ", NodeB: " + nodeB.nodeNumber;
        }
    }

} // Namespace
