using System.Collections.Generic;
using UnityEngine;

namespace MicroSense
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class LevelExit : MonoBehaviour
    {
        [SerializeField, Min(1)] private int _requiredPlayers = 2;

        private readonly HashSet<PlayerController> _playersInside = new HashSet<PlayerController>();

        public int PlayersInside => _playersInside.Count;

        private void OnTriggerEnter2D(Collider2D other)
        {
            RegisterPlayer(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            RegisterPlayer(other);
        }

        private void RegisterPlayer(Collider2D other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null)
            {
                return;
            }

            _playersInside.Add(player);
            if (_playersInside.Count >= _requiredPlayers)
            {
                GameManager.Instance?.CompleteLevel();
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                _playersInside.Remove(player);
            }
        }

        public void Configure(int requiredPlayers)
        {
            _requiredPlayers = Mathf.Max(1, requiredPlayers);
        }

        public void ResetExit()
        {
            _playersInside.Clear();
        }
    }
}
