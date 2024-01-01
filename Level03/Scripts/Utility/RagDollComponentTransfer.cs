using System.Collections.Generic;
using BehaviorDesigner.Runtime.Tasks.Unity.UnityString;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Utility
{
    public class RagDollComponentTransfer : MonoBehaviour
    {
        public Transform From;
        public Transform Target;

        [Button]
        public void Transfer() => Transfer(From, Target);

        private Dictionary<string, Transform> _fromTransforms = new();
        private Dictionary<string, string> _jointMap = new();
        private Dictionary<string, Rigidbody> _targetRigidbodyMap = new();
        public void Transfer(Transform fromRoot, Transform targetRoot)
        {
            Debug.Log($"<color=yellow>Transferring ... {fromRoot} to {targetRoot}</color>");
            _fromTransforms.Clear();
            _jointMap.Clear();
            _targetRigidbodyMap.Clear();

            // 등록
            foreach (var t in fromRoot.GetComponentsInChildren<Transform>())
            {
                if(t == fromRoot) continue;

                var fromName = t.name;
                _fromTransforms.Add(fromName, t);
                Debug.Log($"- {fromName} registered", t);
                
                if (t.TryGetComponent(out CharacterJoint joint))
                {
                    var connected = joint.connectedBody;
                    if (connected)
                    {
                        Debug.Log($"  * {fromName} connected to {connected.name}", connected);
                        _jointMap.Add(fromName, connected.name);
                    }
                }
            }

            // 설정
            foreach (var targetTransform in targetRoot.GetComponentsInChildren<Transform>())
            {
                if(targetTransform == targetRoot) continue;

                var targetObject = targetTransform.gameObject;
                var targetName = targetTransform.name;
                if (!_fromTransforms.TryGetValue(targetName, out var fromTransform))
                {
                    continue;
                }
                Debug.Log($"- {targetName} transferring ...", targetTransform);

                // copy collider
                if (fromTransform.TryGetComponent(out CapsuleCollider fromCapsuleCollider))
                {
                    var targetCollider = targetObject.GetOrAddComponent<CapsuleCollider>();
                    targetCollider.isTrigger = fromCapsuleCollider.isTrigger;
                    targetCollider.center = fromCapsuleCollider.center;
                    targetCollider.providesContacts = fromCapsuleCollider.providesContacts;
                    targetCollider.sharedMaterial = fromCapsuleCollider.sharedMaterial;
                    targetCollider.radius = fromCapsuleCollider.radius;
                    targetCollider.height = fromCapsuleCollider.height;
                    targetCollider.direction = fromCapsuleCollider.direction;
                    Debug.Log($"  * CapsuleCollider transferred", targetCollider);
                }
                if (fromTransform.TryGetComponent(out BoxCollider fromBoxCollider))
                {
                    var targetCollider = targetObject.GetOrAddComponent<BoxCollider>();
                    targetCollider.isTrigger = fromBoxCollider.isTrigger;
                    targetCollider.center = fromBoxCollider.center;
                    targetCollider.providesContacts = fromBoxCollider.providesContacts;
                    targetCollider.sharedMaterial = fromBoxCollider.sharedMaterial;
                    targetCollider.size = fromBoxCollider.size;
                    Debug.Log($"  * BoxCollider transferred", targetCollider);
                }
                
                // copy rigidbody
                if (fromTransform.TryGetComponent(out Rigidbody fromRigidbody))
                {
                    var targetRigidbody = targetObject.GetOrAddComponent<Rigidbody>();
                    targetRigidbody.mass = fromRigidbody.mass;
                    targetRigidbody.drag = fromRigidbody.drag;
                    targetRigidbody.angularDrag = fromRigidbody.angularDrag;
                    targetRigidbody.automaticCenterOfMass = fromRigidbody.automaticCenterOfMass;
                    targetRigidbody.automaticInertiaTensor = fromRigidbody.automaticInertiaTensor;
                    targetRigidbody.useGravity = fromRigidbody.useGravity;
                    targetRigidbody.isKinematic = fromRigidbody.isKinematic;
                    targetRigidbody.interpolation = fromRigidbody.interpolation;
                    targetRigidbody.collisionDetectionMode = fromRigidbody.collisionDetectionMode;
                    targetRigidbody.constraints = fromRigidbody.constraints;
                    _targetRigidbodyMap.Add(targetName, targetRigidbody);
                    Debug.Log($"  * Rigidbody transferred", targetRigidbody);
                }
                
                // copy character joint
                if (fromTransform.TryGetComponent(out CharacterJoint fromJoint))
                {
                    var targetJoint = targetObject.GetOrAddComponent<CharacterJoint>();
                    targetJoint.anchor = fromJoint.anchor;
                    targetJoint.axis = fromJoint.axis;
                    targetJoint.autoConfigureConnectedAnchor = fromJoint.autoConfigureConnectedAnchor;
                    targetJoint.connectedAnchor = fromJoint.connectedAnchor;
                    targetJoint.swingAxis = fromJoint.swingAxis;
                    targetJoint.twistLimitSpring = fromJoint.twistLimitSpring;
                    targetJoint.lowTwistLimit = fromJoint.lowTwistLimit;
                    targetJoint.highTwistLimit = fromJoint.highTwistLimit;
                    targetJoint.swingLimitSpring = fromJoint.swingLimitSpring;
                    targetJoint.swing1Limit = fromJoint.swing1Limit;
                    targetJoint.swing2Limit = fromJoint.swing2Limit;
                    targetJoint.enableProjection = fromJoint.enableProjection;
                    targetJoint.projectionDistance = fromJoint.projectionDistance;
                    targetJoint.projectionAngle = fromJoint.projectionAngle;
                    targetJoint.breakForce = fromJoint.breakForce;
                    targetJoint.breakTorque = fromJoint.breakTorque;
                    targetJoint.enableCollision = fromJoint.enableCollision;
                    targetJoint.enablePreprocessing = fromJoint.enablePreprocessing;
                    targetJoint.massScale = fromJoint.massScale;
                    targetJoint.connectedMassScale = fromJoint.connectedMassScale;
                    Debug.Log($"  * CharacterJoint transferred", targetJoint);

                    if (!_jointMap.TryGetValue(targetName, out var connectedBodyName))
                    {
                        Debug.Log($"<color=red>{targetName} has joint but no connect body</color>", targetJoint);
                    }
                    else if (!_targetRigidbodyMap.TryGetValue(connectedBodyName, out var targetRigidbody))
                    {
                        Debug.Log($"<color=red>{targetName} has joint but cannot find connected rigidbody {connectedBodyName}</color>", targetJoint);
                    }
                    else
                    {
                        Debug.Log($"  * CharacterJoint connected body set to {connectedBodyName}", targetJoint);
                        targetJoint.connectedBody = targetRigidbody;
                    }
                }
            }
            Debug.Log($"<color=green>Transfer Completed {fromRoot} to {targetRoot}</color>");
        }
    }
}