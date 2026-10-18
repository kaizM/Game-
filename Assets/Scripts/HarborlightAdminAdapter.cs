using System.Collections.Generic;
using Meridian.GameAdmin;
using UnityEngine;

namespace Harborlight
{
    public sealed class HarborlightAdminAdapter : GameAdminAdapterBehaviour
    {
        private static readonly IReadOnlyList<AdminActionDefinition> Actions = new List<AdminActionDefinition>
        {
            new AdminActionDefinition("credits", "Add 500 credits", "500"),
            new AdminActionDefinition("reputation", "Add 5 reputation"),
            new AdminActionDefinition("teleport_square", "Teleport to Lantern Square"),
            new AdminActionDefinition("teleport_pier", "Teleport to North Pier"),
            new AdminActionDefinition("reset_contract", "Reset beacon contract"),
            new AdminActionDefinition("reset_courier", "Reset courier job"),
            new AdminActionDefinition("reset_inspection", "Reset pier inspection"),
            new AdminActionDefinition("reset_save", "Reset local progress")
        };

        private HarborlightGameState _state;

        public override string GameId => "harborlight-city";
        public override string DisplayName => "Harborlight · North Pier";

        public void Bind(HarborlightGameState state)
        {
            _state = state;
        }

        public override GameAdminSnapshot GetSnapshot()
        {
            return new GameAdminSnapshot
            {
                gameId = GameId,
                displayName = DisplayName,
                status = _state != null ? _state.StatusLabel() : "Loading",
                location = _state != null ? _state.CurrentLocation : "Unknown",
                credits = _state != null ? _state.Credits : 0,
                reputation = _state != null ? _state.Reputation : 0,
                updatedAtUtc = System.DateTime.UtcNow.ToString("O"),
                recentActions = _state != null ? _state.RecentAdminActions : new string[0]
            };
        }

        public override IReadOnlyList<AdminActionDefinition> GetActions() => Actions;

        public override bool TryExecute(string actionId, string payload, out string result)
        {
            if (_state == null)
            {
                result = "Game state is not ready.";
                return false;
            }
            return _state.ExecuteAdminAction(actionId, payload, out result);
        }
    }
}
