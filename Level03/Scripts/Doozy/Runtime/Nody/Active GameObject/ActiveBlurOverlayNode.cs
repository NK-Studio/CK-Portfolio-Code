// Copyright (c) 2015 - 2023 Doozy Entertainment. All Rights Reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using System;
using Doozy.Runtime.Nody.Nodes.Internal;
using NKStudio;
using UnityEngine;
// ReSharper disable RedundantOverriddenMember

namespace Doozy.Runtime.Nody.Nodes
{
    [Serializable]
    [NodyMenuPath("Custom", "Active Blur Overlay")] // <<< Change search menu options here category and node name
    public sealed class ActiveBlurOverlayNode : SimpleNode
    {
        public bool ActiveBlurOverlay;
        
        public ActiveBlurOverlayNode()
        {
            AddInputPort()                 // add a new input port
                .SetCanBeDeleted(false)    // port options
                .SetCanBeReordered(false); // port options

            AddOutputPort()                // add a new output port
                .SetCanBeDeleted(false)    // port options
                .SetCanBeReordered(false); // port options

            canBeDeleted = true;           // Used to prevent special nodes from being deleted in the editor
            
            runUpdate = false;             // Run Update when the node is active
            runFixedUpdate = false;        // Run FixedUpdate when the node is active
            runLateUpdate = false;         // Run LateUpdate when the node is active
            
            passthrough = true;            //allow the graph to bypass this node when going back
                
            clearGraphHistory = false;     //remove the possibility of being able to go back to previously active nodes
        }

        // Called on the frame when this node becomes active
        public override void OnEnter(FlowNode previousNode = null, FlowPort previousPort = null)
        {
            base.OnEnter(previousNode, previousPort);
            Run();                         //do something
            GoToNextNode(firstOutputPort); //immediately go to the next node
        }
        
        private void Run()
        {
            // 오브젝트들을 활성화/비활성화 합니다.
            if (ActiveBlurOverlay)
                Messager.Send("ShowBlur");
            else
                Messager.Send("HideBlur");
        }

        // Called just before this node becomes idle
        public override void OnExit()
        {
            base.OnExit();
            //do on exit
        }

        // Called when the parent graph started and this is global node
        public override void Start()
        {
            base.Start();
            //do on start (for global nodes)
        }

        // Called when the parent graph stopped and this is a global node
        public override void Stop()
        {
            base.Stop();
            //do on stop (for global nodes)
        }

        // Called every frame, if the node is enabled and active (Update Method)
        public override void Update()
        {
            base.Update();
            //do on Update
        }

        // Called every frame, if the node is enabled and active (FixedUpdate Method)
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            //do on FixedUpdate
        }

        // Called every frame, if the node is enabled and active (LateUpdate Method)
        public override void LateUpdate()
        {
            base.LateUpdate();
            //do on LateUpdate
        }

        // Clone this node
        public override FlowNode Clone()
        {
            return base.Clone();
            //clone custom settings
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

        // Override - Add a new output port to this node
        public override FlowPort AddOutputPort(PortCapacity capacity = PortCapacity.Single)
        {
            FlowPort port = base.AddOutputPort(capacity);
            //add output port value
            return port;
        }
    }
}
