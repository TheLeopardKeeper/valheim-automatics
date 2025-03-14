﻿using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ModUtils;
using UnityEngine;

namespace Automatics.AutomaticDoor
{
    [DisallowMultipleComponent]
    internal class AutomaticDoor : MonoBehaviour
    {
        private static readonly Lazy<int> LazyPieceMask;

        private static int PieceMask => LazyPieceMask.Value;

        static AutomaticDoor()
        {
            LazyPieceMask = new Lazy<int>(() =>
                LayerMask.GetMask("Default", "static_solid", "Default_small", "piece",
                    "piece_nonsolid", "terrain", "vehicle"));
        }

        private Door _door;
        private ZNetView _zNetView;
        private float _openTimer;
        private float _closeTimer;

        private void Awake()
        {
            _door = GetComponent<Door>();
            Objects.GetZNetView(_door, out _zNetView);

            if (_zNetView.IsValid() && _zNetView.IsOwner())
                StartCoroutine(nameof(UpdateAutomaticDoor));
        }

        private void OnDestroy()
        {
            if (_zNetView.IsValid() && _zNetView.IsOwner())
                StopCoroutine(nameof(UpdateAutomaticDoor));

            _door = null;
            _zNetView = null;
        }

        [SuppressMessage("ReSharper", "IteratorNeverReturns")]
        private IEnumerator UpdateAutomaticDoor()
        {
            while (true)
            {
                var active = false;
                while (!active)
                {
                    yield return new WaitForSeconds(0.1f);

                    if (!Config.EnableAutomaticDoor) continue;
                    if (!Logics.IsAllowAutomaticDoor(_door)) continue;
                    if (_zNetView.GetZDO() == null || !IsOwner()) continue;
                    active = true;
                }

                if (Time.time - _openTimer >= Config.IntervalToOpen)
                {
                    TryOpen();
                    _openTimer = Time.time;
                    yield return null;
                }

                if (Time.time - _closeTimer >= Config.IntervalToClose)
                {
                    TryClose();
                    _closeTimer = Time.time;
                    yield return null;
                }
            }
        }

        private void TryOpen()
        {
            if (IsDoorOpen()) return;
            var (player, distance) = GetClosestPlayer();
            if (!player || distance > Config.DistanceForAutomaticOpening) return;
            if (IsExistsObstaclesBetweenTo(player) || !CanInteract(player)) return;
            if (_door.m_keyItem)
                player.Message(MessageHud.MessageType.Center,
                    Localization.instance.Localize("$msg_door_usingkey",
                        _door.m_keyItem.m_itemData.m_shared.m_name));
            Reflections.InvokeMethod(_door, "Open", (player.transform.position - _door.transform.position).normalized);
        }

        private void TryClose()
        {
            if (!IsDoorOpen()) return;
            var (player, distance) = GetClosestPlayer();
            if (!player || distance <= Config.DistanceForAutomaticClosing) return;
            if (IsExistsObstaclesBetweenTo(player) || !CanInteract(player)) return;
            Reflections.InvokeMethod(_door, "Open", (player.transform.position - _door.transform.position).normalized);
        }

        private bool IsOwner()
        {
            return _zNetView.IsValid() && _zNetView.IsOwner();
        }

        private bool IsDoorOpen()
        {
            return _zNetView.GetZDO().GetInt("state") != 0;
        }

        private bool CanInteract(Player player)
        {
            if (_door.m_checkGuardStone && !PrivateArea.CheckAccess(_door.transform.position))
                return false;
            if (!Reflections.InvokeMethod<bool>(_door, "CanInteract")) return false;
            return !_door.m_keyItem || Reflections.InvokeMethod<bool>(_door, "HaveKey", player);
        }

        private (Player player, float distance) GetClosestPlayer()
        {
            var origin = _door.transform.position;
            return (from x in Player.GetAllPlayers()
                    let distance = Vector3.Distance(origin, x.transform.position)
                    orderby distance
                    select (x, distance))
                .FirstOrDefault();
        }

        private bool IsExistsObstaclesBetweenTo(Player player)
        {
            var from = Reflections.GetField<Collider>(player, "m_collider")?.bounds.center ??
                       player.m_eye.position;
            var to = _door.transform.position;

            if (!Physics.Linecast(from, to, out var hitInfo, PieceMask)) return false;

            var hitDoor = hitInfo.collider.GetComponentInParent<Door>();
            if (hitDoor) return hitDoor != _door;

            return hitInfo.collider.GetComponentInParent<Piece>();
        }
    }
}