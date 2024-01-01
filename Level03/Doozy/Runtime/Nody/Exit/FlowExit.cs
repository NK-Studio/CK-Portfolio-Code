using System;
using Doozy.Runtime.Nody.Nodes.Internal;
using Doozy.Runtime.UIManager.Input;
using Managers;
using ManagerX;
using UnityEngine;


namespace Doozy.Runtime.Nody.Nodes
{
    [Serializable]
    [NodyMenuPath("Custom", "FlowExit")] // <<< Change search menu options here category and node name
    public sealed class FlowExit : SimpleNode
    {
        public FlowExit()
        {
            AddInputPort()                 // add a new input port
                .SetCanBeDeleted(false)    // port options
                .SetCanBeReordered(false); // port options

            nodeDescription = "Start 노드로 이동합니다."; //node description
            
            canBeDeleted = true;           // Used to prevent special nodes from being deleted in the editor
            clearGraphHistory = false;     //remove the possibility of being able to go back to previously active nodes
        }

        // Called on the frame when this node becomes active
        public override void OnEnter(FlowNode previousNode = null, FlowPort previousPort = null)
        {
            base.OnEnter(previousNode, previousPort);
            Run();                         //do something
        }
        
        private void Run()
        {
            //do stuff
            AutoManager.Get<GameManager>().IsActiveMenu = false;
            flowGraph.SetActiveNodeByNodeName("Start");
        }
        
        // Override - Add a new port to this node
        public override FlowPort AddPort(PortDirection direction, PortCapacity capacity)
        {
            FlowPort port = base.AddPort(direction, capacity);
            //add port value
            return port;
        }

        // Override - Add a new input port to this node
        public override FlowPort AddInputPort(PortCapacity capacity = PortCapacity.Multi)
        {
            FlowPort port = base.AddInputPort(capacity);
            //add input port value
            return port;
        }
    }
}
