using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Harborlight
{
    public sealed class HarborlightGarageService : MonoBehaviour
    {
        private HarborlightGameState _state;
        private HarborlightBackendClient _backend;
        private IReadOnlyList<HarborlightVehicle> _vehicles = new HarborlightVehicle[0];
        private bool _ownershipSynced;

        public IReadOnlyList<HarborlightBackendClient.Cosmetic> Catalog
            => _backend != null ? _backend.Catalog : Array.Empty<HarborlightBackendClient.Cosmetic>();

        public int SelectedVehicleIndex => _state != null ? _state.SelectedVehicleIndex : 0;
        public int VehicleCount => _vehicles.Count;
        public string SelectedVehicleName => SelectedVehicleIndex >= 0 && SelectedVehicleIndex < _vehicles.Count
            ? _vehicles[SelectedVehicleIndex].DisplayName : "Vehicle";

        public void Bind(HarborlightGameState state, HarborlightBackendClient backend, IReadOnlyList<HarborlightVehicle> vehicles)
        {
            _state = state;
            _backend = backend;
            _vehicles = vehicles ?? new HarborlightVehicle[0];
            ApplySavedPaints();
        }

        private void Update()
        {
            if (_backend == null || !_backend.IsConnected) return;
            var remoteOwnership = _backend.OwnedCosmetics.Select(item => item.sku).ToArray();
            if (!_ownershipSynced || !new HashSet<string>(_state.OwnedCosmeticSkus).SetEquals(remoteOwnership))
            {
                _state.SetCosmeticOwnership(remoteOwnership);
                _ownershipSynced = true;
                ApplySavedPaints();
            }
        }

        public void SelectVehicle(int index)
        {
            if (_state == null || index < 0 || index >= _vehicles.Count || !_state.IsVehicleOwned(index)) return;
            _state.SelectGarageVehicle(index);
        }

        public string VehicleChoiceLabel(int index)
        {
            if (index < 0 || index >= _vehicles.Count || index >= HarborlightGameState.VehicleCatalogSize) return "Unavailable";
            var label = index == 0 ? "CAB" : index == 1 ? "SEDAN" : index == 2 ? "VAN"
                : index == 3 ? "HATCH" : index == 4 ? "PICKUP" : index == 5 ? "SPORT"
                : index == 6 ? "EXOTIC" : "GRAND TOURER";
            var owned = _state != null && _state.IsVehicleOwned(index);
            return owned
                ? (index == SelectedVehicleIndex ? "✓ " : "") + label
                : label + "  ¢" + HarborlightGameState.VehiclePrice(index);
        }

        public string SelectOrBuyVehicle(int index)
        {
            if (_state == null || index < 0 || index >= _vehicles.Count) return "Garage is not ready.";
            if (_state.IsVehicleOwned(index))
            {
                _state.SelectGarageVehicle(index);
                var selected = _vehicles[index].DisplayName + " selected.";
                _state.ReportMessage(selected);
                return selected;
            }

            _state.TryPurchaseVehicle(index, out var result);
            _state.ReportMessage(result);
            return result;
        }

        public bool CanUseVehicle(int index) => _state != null && _state.IsVehicleOwned(index);

        public void RestoreFactoryPaint()
        {
            if (_state == null || SelectedVehicleIndex >= _vehicles.Count) return;
            _state.SetVehiclePaint(SelectedVehicleIndex, "");
            _vehicles[SelectedVehicleIndex].RestoreFactoryPaint();
            _state.ReportMessage(SelectedVehicleName + " restored to its factory finish.");
        }

        public bool IsOwned(string sku) => _state != null && _state.OwnsCosmetic(sku);

        public void ChoosePaint(HarborlightBackendClient.Cosmetic cosmetic, Action<bool, string> completed)
        {
            if (_state == null || cosmetic == null || SelectedVehicleIndex >= _vehicles.Count)
            {
                completed?.Invoke(false, "Garage is not ready.");
                return;
            }

            if (IsOwned(cosmetic.sku))
            {
                Equip(cosmetic.sku);
                completed?.Invoke(true, cosmetic.displayName + " applied.");
                return;
            }

            if (_backend == null || !_backend.IsConnected)
            {
                completed?.Invoke(false, "Connect to the Harborlight account service to unlock finishes.");
                return;
            }

            _backend.SpendCosmetic(cosmetic.sku, (succeeded, result) =>
            {
                if (succeeded)
                {
                    SyncOwnership();
                    Equip(cosmetic.sku);
                }
                completed?.Invoke(succeeded, result);
            });
        }

        private void SyncOwnership()
        {
            if (_backend == null) return;
            _state.SetCosmeticOwnership(_backend.OwnedCosmetics.Select(item => item.sku));
            _ownershipSynced = true;
        }

        private void Equip(string sku)
        {
            if (!_state.SetVehiclePaint(SelectedVehicleIndex, sku)) return;
            _vehicles[SelectedVehicleIndex].SetPaintColor(PaintColor(sku));
            var name = Catalog.FirstOrDefault(item => item.sku == sku)?.displayName ?? sku;
            _state.ReportMessage(name + " applied to " + SelectedVehicleName + ".");
        }

        private void ApplySavedPaints()
        {
            if (_state == null) return;
            for (var i = 0; i < _vehicles.Count; i++)
            {
                var sku = _state.GetVehiclePaintSku(i);
                if (_state.OwnsCosmetic(sku)) _vehicles[i].SetPaintColor(PaintColor(sku));
                else _vehicles[i].RestoreFactoryPaint();
            }
        }

        private static Color PaintColor(string sku)
        {
            switch (sku)
            {
                case "harbor-paint-sea-glass": return HexColor("#65BFB1");
                case "harbor-paint-copper": return HexColor("#BB6848");
                case "pier-light-warmth": return HexColor("#E5B54F");
                default: return Color.white;
            }
        }

        private static Color HexColor(string value)
        {
            ColorUtility.TryParseHtmlString(value, out var color);
            return color;
        }
    }
}
