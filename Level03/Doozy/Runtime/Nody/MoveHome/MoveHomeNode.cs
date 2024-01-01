// Copyright (c) 2015 - 2023 Doozy Entertainment. All Rights Reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using System;
using Doozy.Runtime.Nody.Nodes.Internal;
using Managers;
using ManagerX;
using SceneSystem;
using UnityEngine;
using SceneManager = UnityEngine.SceneManagement.SceneManager;

// ReSharper disable RedundantOverriddenMember

namespace Doozy.Runtime.Nody.Nodes
{
    [Serializable]
    [NodyMenuPath("Custom", "MoveHome")] // <<< Change search menu options here category and node name
    public sealed class MoveHomeNode : SimpleNode
    {
        public MoveHomeNode()
        {
            AddInputPort()                 // add a new input port
                .SetCanBeDeleted(false)    // port options
                .SetCanBeReordered(false); // port options
            
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
            Time.timeScale = 1;
            AutoManager.Get<GameManager>().IsActiveMenu = false;
            AutoManager.Get<SceneController>().MoveHome();
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
