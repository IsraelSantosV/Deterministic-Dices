using System.Collections;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;

namespace RWS.Utils {
    public class DiceInstance : MonoBehaviour {

        [SerializeField] private float MySize = 0.5f;
        [SerializeField] private float AnimationSpeed = 1;
        [SerializeField] private Vector3[] ResultValues = 
            new Vector3[6] {(1,0,0),(1,0,1),(1,1,0),(0,1,0),(0,1,1),(0,0,1) };

        public int SelectedValue { get; private set; }

        private AnimationClip m_RecorderClip;
        private Animator m_Animator;
        private Renderer m_Renderer;
        private Rigidbody m_Rb;
        private GameObjectRecorder m_Recorder;
        private DiceManager m_Roller;
        private int m_Index;

        private bool m_EnableDetection;
        private bool m_InGround;
        private bool m_IsRecording;

        public bool IsStopped  => m_Rb.velocity == Vector3.zero && m_InGround;
        public int Index => m_Index;
        public float GetMyAnimSpeed() => AnimationSpeed;
        public float GetClipLength() => m_RecorderClip == null ? 0 : m_RecorderClip.length;

        private void Awake() {
            m_Animator = GetComponent<Animator>();
            m_Renderer = GetComponent<MeshRenderer>();
            m_Rb = GetComponent<Rigidbody>();
        }

        private void OnEnable() {
            m_InGround = false;
            m_IsRecording = false;
        }

        /// <summary>
        /// Saves the current dice animation clip and stops the current recording
        /// </summary>
        private void SaveClip() {
            if(m_RecorderClip == null) {
                return;
            }

            if (m_Recorder.isRecording) {
                m_Recorder.SaveToClip(m_RecorderClip);
            }

            m_IsRecording = false;
        }

        /// <summary>
        /// Initializes the current dice instance, placing as animation clip the 
        /// first one found in the animation hierarchy
        /// </summary>
        /// <param name="roller"></param>
        /// <param name="index"> Dice ID </param>
        /// <param name="controller"></param>
        public void SetRoller(DiceManager roller, int index, RuntimeAnimatorController controller) {
            m_Roller = roller;
            m_Index = index;
            m_Animator.runtimeAnimatorController = controller;

            var runtimeAnim = m_Animator.runtimeAnimatorController;
            for (int i = 0; i < runtimeAnim.animationClips.Length; i++) {
                if (runtimeAnim.animationClips[i] != null) {
                    m_RecorderClip = runtimeAnim.animationClips[i];
                    break;
                }
            }
        }

        /// <summary>
        /// Method that initializes invisible dice rotation
        /// </summary>
        /// <param name="launchPosition">Initial position where the index will start the rotation</param>
        /// <param name="forceRange">Force at which the dice will be rotated</param>
        /// <param name="upForce">Force at which the dice will be thrown up</param>
        public void Roll(Vector3 launchPosition, Vector2 forceRange, float upForce) {
            float dirX = Random.Range(forceRange.x, forceRange.y);
            float dirY = Random.Range(forceRange.x, forceRange.y);
            float dirZ = Random.Range(forceRange.x, forceRange.y);

            m_InGround = false;

            transform.position = launchPosition;
            transform.rotation = Quaternion.identity;

            m_Rb.AddForce(transform.up * upForce);
            m_Rb.AddTorque(dirX, dirY, dirZ);
        }

        /// <summary>
        /// When calling this method, the apparent index will be rotated 
        /// according to the animation clip obtained in 'Roll'
        /// </summary>
        public void FakeRoll() {
            m_InGround = false;

            SetRendererState(true);
            SetPhysics(false);
            if(m_Animator != null && m_RecorderClip != null) {
                m_Animator.SetFloat("AnimationSpeed", AnimationSpeed);
                m_Animator.SetTrigger("Roll");
            }
        }

        /// <summary>
        /// Starts clip recording of the current state of the dice
        /// </summary>
        public void RecordMovement() {
            m_Recorder = new GameObjectRecorder(gameObject);
            m_Recorder.BindComponentsOfType<Transform>(gameObject, true);
            m_IsRecording = true;
        }

        /// <summary>
        /// Gets at each frame, if the dice has physics enabled, 
        /// the value that is face up using scalar product
        /// </summary>
        private void Update() {
            if (!m_EnableDetection) return;

            float bestDot = -1;
            for (int i = 0; i < ResultValues.Length; i++) {
                Vector3 valueVector = ResultValues[i];
                var worldSpaceValueVector = transform.localToWorldMatrix.MultiplyVector(valueVector);

                if(Physics.Raycast(transform.position, valueVector, MySize)) {
                    m_InGround = true;
                }

                float dot = Vector3.Dot(worldSpaceValueVector, Vector3.up);
                if(dot > bestDot) {
                    bestDot = dot;
                    SelectedValue = i + 1;
                }
            }
        }

        private void LateUpdate() {
            if (!m_IsRecording || m_RecorderClip == null) return;

            m_Recorder.TakeSnapshot(Time.fixedDeltaTime);
        }

        public void FinishRecording() {
            m_EnableDetection = false;

            if (m_IsRecording) {
                SaveClip();
                m_IsRecording = false;
            } 
        }

        public void SetRendererState(bool visible) {
            m_Renderer.enabled = visible;
        }

        public void SetPhysics(bool enable) {
            m_Animator.enabled = !enable;
            m_Rb.isKinematic = !enable;
            m_Rb.useGravity = enable;
            m_EnableDetection = enable;
        }

        //Internal debug view
        private void OnDrawGizmos() {
            Gizmos.color = Color.red;
            foreach(var valueVector in ResultValues) {
                var worldSpaceValueVector = transform.localToWorldMatrix.MultiplyVector(valueVector);
                Gizmos.DrawLine(transform.position, transform.position + worldSpaceValueVector);
            }
        }

    }
}
