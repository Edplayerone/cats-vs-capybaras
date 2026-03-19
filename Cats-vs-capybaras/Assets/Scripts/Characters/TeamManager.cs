using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatsVsCapybaras
{
    /// <summary>
    /// Manages team composition, character registration, alive-tracking,
    /// round-robin turn order, and per-team weapon ammo.
    /// Supports an arbitrary number of teams with any number of characters per team.
    /// </summary>
    public class TeamManager : MonoBehaviour
    {
        [Serializable]
        public class TeamConfig
        {
            public string teamName = "Team";
            public Color teamColor = Color.white;
            public CharacterController2D[] characters = Array.Empty<CharacterController2D>();
        }

        public event Action<int, CharacterController2D> OnCharacterEliminated;
        public event Action<int> OnTeamDefeated;

        [SerializeField] private TeamConfig[] teams = Array.Empty<TeamConfig>();

        /// <summary>Per-team round-robin index tracking for turn order.</summary>
        private int[] turnIndices;

        /// <summary>Per-team ammo arrays. Key = teamIndex, value = ammo per weapon slot.</summary>
        private Dictionary<int, int[]> teamAmmo;

        public int TeamCount => teams.Length;

        /// <summary>
        /// Initializes team data structures and subscribes to character events.
        /// Called by GameManager during game start.
        /// </summary>
        public void Initialize()
        {
            turnIndices = new int[teams.Length];

            for (int t = 0; t < teams.Length; t++)
            {
                turnIndices[t] = -1;

                for (int c = 0; c < teams[t].characters.Length; c++)
                {
                    var character = teams[t].characters[c];
                    if (character == null) continue;

                    int capturedTeam = t;
                    character.OnEliminated += ch => HandleEliminated(capturedTeam, ch);
                }
            }
        }

        /// <summary>
        /// Sets up per-team ammo pools from the weapon definitions.
        /// </summary>
        public void InitializeAmmo(WeaponData[] weapons)
        {
            teamAmmo = new Dictionary<int, int[]>();
            for (int t = 0; t < teams.Length; t++)
            {
                int[] ammo = new int[weapons.Length];
                for (int w = 0; w < weapons.Length; w++)
                    ammo[w] = weapons[w].startingAmmo;
                teamAmmo[t] = ammo;
            }
        }

        public bool HasAmmo(int teamIndex, int weaponIndex)
        {
            if (teamAmmo == null || !teamAmmo.ContainsKey(teamIndex)) return true;
            int a = teamAmmo[teamIndex][weaponIndex];
            return a == -1 || a > 0;
        }

        public void ConsumeAmmo(int teamIndex, int weaponIndex)
        {
            if (teamAmmo == null || !teamAmmo.ContainsKey(teamIndex)) return;
            if (teamAmmo[teamIndex][weaponIndex] > 0)
                teamAmmo[teamIndex][weaponIndex]--;
        }

        public int GetAmmo(int teamIndex, int weaponIndex)
        {
            if (teamAmmo == null || !teamAmmo.ContainsKey(teamIndex)) return -1;
            return teamAmmo[teamIndex][weaponIndex];
        }

        // ── Queries ────────────────────────────────────────────────

        public string GetTeamName(int teamIndex)
        {
            return IsValidTeam(teamIndex) ? teams[teamIndex].teamName : "Unknown";
        }

        public Color GetTeamColor(int teamIndex)
        {
            return IsValidTeam(teamIndex) ? teams[teamIndex].teamColor : Color.white;
        }

        public int GetCharacterCount(int teamIndex)
        {
            return IsValidTeam(teamIndex) ? teams[teamIndex].characters.Length : 0;
        }

        public CharacterController2D GetCharacter(int teamIndex, int charIndex)
        {
            if (!IsValidTeam(teamIndex)) return null;
            var chars = teams[teamIndex].characters;
            return charIndex >= 0 && charIndex < chars.Length ? chars[charIndex] : null;
        }

        public List<CharacterController2D> GetAliveCharacters(int teamIndex)
        {
            var alive = new List<CharacterController2D>();
            if (!IsValidTeam(teamIndex)) return alive;

            foreach (var ch in teams[teamIndex].characters)
            {
                if (ch != null && ch.IsAlive)
                    alive.Add(ch);
            }
            return alive;
        }

        public bool IsTeamAlive(int teamIndex)
        {
            if (!IsValidTeam(teamIndex)) return false;
            foreach (var ch in teams[teamIndex].characters)
            {
                if (ch != null && ch.IsAlive) return true;
            }
            return false;
        }

        /// <summary>
        /// Returns the next alive character on the given team using round-robin order.
        /// Returns -1 if no alive characters remain.
        /// </summary>
        public int GetNextAliveCharacterIndex(int teamIndex)
        {
            if (!IsValidTeam(teamIndex)) return -1;
            int count = teams[teamIndex].characters.Length;
            int start = turnIndices[teamIndex];

            for (int i = 0; i < count; i++)
            {
                int idx = (start + 1 + i) % count;
                var ch = teams[teamIndex].characters[idx];
                if (ch != null && ch.IsAlive)
                {
                    turnIndices[teamIndex] = idx;
                    return idx;
                }
            }
            return -1;
        }

        /// <summary>
        /// Returns the next team index (after the given one) that still has alive characters.
        /// Returns -1 if no other teams are alive.
        /// </summary>
        public int GetNextAliveTeam(int afterTeam)
        {
            for (int i = 1; i <= teams.Length; i++)
            {
                int t = (afterTeam + i) % teams.Length;
                if (IsTeamAlive(t)) return t;
            }
            return -1;
        }

        /// <summary>
        /// Returns the winning team index if exactly one team remains alive.
        /// Returns -2 if no teams are alive (draw), or -1 if the game should continue.
        /// </summary>
        public int CheckWinCondition()
        {
            int aliveCount = 0;
            int lastAlive = -2;

            for (int t = 0; t < teams.Length; t++)
            {
                if (IsTeamAlive(t))
                {
                    aliveCount++;
                    lastAlive = t;
                }
            }

            if (aliveCount == 1) return lastAlive;
            if (aliveCount == 0) return -2;
            return -1;
        }

        // ── Reset ──────────────────────────────────────────────────

        public void ResetAllCharacters()
        {
            for (int t = 0; t < teams.Length; t++)
            {
                turnIndices[t] = -1;
                foreach (var ch in teams[t].characters)
                {
                    if (ch != null) ch.ResetForNewRound();
                }
            }
        }

        // ── Internal ───────────────────────────────────────────────

        private void HandleEliminated(int teamIndex, CharacterController2D character)
        {
            OnCharacterEliminated?.Invoke(teamIndex, character);

            if (!IsTeamAlive(teamIndex))
                OnTeamDefeated?.Invoke(teamIndex);
        }

        private bool IsValidTeam(int index)
        {
            return index >= 0 && index < teams.Length;
        }
    }
}
