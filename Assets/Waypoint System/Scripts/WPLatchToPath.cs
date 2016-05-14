using UnityEngine;
using System.Collections;

namespace Waypoints
{
    public class WPLatchToPath : MonoBehaviour
    {
        [SerializeField] WPNode latchNode;
        [SerializeField] bool reverse;

        void Latch()
        {
            this.transform.position = latchNode.transform.position;
            //GetComponent<WPTraverser>().TraversePath(latchNode, 1);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                Latch();
            }
        }
    }
}
