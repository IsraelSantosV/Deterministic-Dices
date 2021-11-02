using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;
using RWS.UI;

namespace RWS.Utils {
    public class DiceManager : MonoBehaviour {

        public static DiceManager Instance;

        //Create unique Animator and AnimationClip for once dice instance!

        [Header("Essentials")]
        [SerializeField] private DiceInstance DicePrefab;
        [SerializeField] private Transform StartPosition;
        [SerializeField] private Vector2 ForceRange;
        [SerializeField] private float UpForce = 800;
        [SerializeField] private RuntimeAnimatorController[] AllControllers;
        [SerializeField] private int MaxDices = 2;

        [Header("Misc")]
        [SerializeField] private GameObject[] m_EnvironmentDices;
        [SerializeField] private Sprite[] m_DicesFaces;

        private DiceInstance[] m_Dices;
        private int[] m_DiceResults;

        public int[] DiceResults => m_DiceResults;
        public int GetMaxDices() => MaxDices;

        /// <summary>
        /// Initializes the dices, where each one is created and disabled
        /// </summary>
        private void Awake() {
            if(Instance != null && Instance != this) {
                Destroy(Instance.gameObject);
            }

            Instance = this;

            m_Dices = new DiceInstance[MaxDices];
            m_DiceResults = new int[MaxDices];

            if(AllControllers.Length != MaxDices) {
                Debug.LogError("Amount of Animator Controllers must have a same amount of dices!");
                return;
            }

            for(int i = 0; i < MaxDices; i++) {
                m_Dices[i] = Instantiate(DicePrefab, StartPosition.position, StartPosition.rotation);
                m_Dices[i].SetRoller(this, i, AllControllers[i]);
                m_Dices[i].SetRendererState(false);
                m_Dices[i].SetPhysics(false);
            }        
        }

        /// <summary>
        /// Resets the current dices and roll the specified number of dices. 
        /// Attention: This is a routine call that uses More Effective Coroutines, 
        /// it should be called as follows: Timing.RunCoroutine(_Roll(amount));
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        public IEnumerator<float> _Roll(int amount) {
            ResetDiceValues();
            var targetAmount = amount > MaxDices ? MaxDices : amount;
            yield return Timing.WaitUntilDone(Timing.RunCoroutine(_RollAction(targetAmount)));
        }

        /// <summary>
        /// An 'amount' amount of dices are rotated with 'invisible' data, 
        /// recording every move made by them. Must be called before _RollFakeDices
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        private IEnumerator<float> _RollAction(int amount) {
            for(int i = 0; i < amount; i++) {
                m_Dices[i].SetRendererState(false);
                m_Dices[i].SetPhysics(true);
                m_Dices[i].RecordMovement();
                m_Dices[i].Roll(StartPosition.position, ForceRange, UpForce);
            }

            while (true) {
                var stoppedAmount = 0;
                for (int i = 0; i < amount; i++) {
                    if (m_Dices[i].IsStopped) {
                        stoppedAmount++;
                    }
                }

                if (stoppedAmount >= amount) {
                    break;
                }

                yield return Timing.WaitForOneFrame;
            }

            //Register Results
            for (int i = 0; i < amount; i++) {
                RegisterResult(m_Dices[i].SelectedValue, m_Dices[i].Index);
            }
        }

        /// <summary>
        /// The fake dices are released to illustrate the movement of the 'invisible' 
        /// dices. The routine time to be expected is the time of the longest rotation animation
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        public IEnumerator<float> _RollFakeDices(int amount) {
            SetEnvironmentVisibility(MaxDices - amount);
            //Discovery wait time
            var biggerAnimationTime = 0f;
            for (int i = 0; i < amount; i++) {
                var length = m_Dices[i].GetClipLength() / m_Dices[i].GetMyAnimSpeed();
                if (length > biggerAnimationTime) {
                    biggerAnimationTime = length;
                }

                m_Dices[i].FinishRecording();
                Timing.RunCoroutine(_RollFakeAction(m_Dices[i]));
            }

            yield return Timing.WaitForSeconds(biggerAnimationTime);
        }

        private IEnumerator<float> _RollFakeAction(DiceInstance dice) {
            var duration = dice.GetClipLength() / dice.GetMyAnimSpeed();
            dice.FakeRoll();

            var timer = 0f;
            while (timer < duration) {
                timer += Time.deltaTime;
                yield return Timing.WaitForOneFrame;
            }
        }

        /// <summary>
        /// Results are obtained before false dices are posted.
        /// </summary>
        /// <param name="result"></param>
        /// <param name="targetDice"></param>
        private void RegisterResult(int result, int targetDice) {
            if (targetDice < 0 || targetDice >= m_DiceResults.Length) return;
            m_DiceResults[targetDice] = result;
        }

        public void ResetDiceValues() {
            var resetedValues = new List<int>();
            for (int i = 0; i < m_DiceResults.Length; i++) {
                m_DiceResults[i] = -1;
                resetedValues.Add(1);
            }

            foreach(var dice in m_Dices) {
                if(dice != null) {
                    dice.SetRendererState(false);
                    dice.SetPhysics(false);
                }
            }

            SetEnvironmentVisibility(MaxDices);
        }

        /// <summary>
        /// Get the texture of the current face of the dice
        /// </summary>
        /// <param name="value">Face that wants to acquire the texture</param>
        /// <returns></returns>
        public static Sprite GetDiceFace(int value) {
            if (value < 0 || value >= Instance.m_DicesFaces.Length) return null;
            return Instance.m_DicesFaces[value];
        }

        private void SetEnvironmentVisibility(int visibleAmount) {
            for (int i = 0; i < m_EnvironmentDices.Length; i++) {
                GameObject dice = m_EnvironmentDices[i];
                if (dice != null) dice.SetActive((i + 1) <= visibleAmount);
            }
        }

    }
}
