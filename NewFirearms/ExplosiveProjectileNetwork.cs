using System.Collections.Generic;
using UnityEngine;

namespace NewFirearms
{
    // Keep optional transport types out of MonoBehaviour method signatures.
    internal static class ExplosiveProjectileNetwork
    {
        internal const string MessageId = "NewFirearms_ExplosiveProjectile";
        private static int nextId;
        private static readonly Dictionary<int, ExplosiveProjectile> replicas = new Dictionary<int, ExplosiveProjectile>();

        internal static void Spawn(ExplosiveProjectile projectile)
        {
            projectile.NetworkId = ++nextId;
            Sync(projectile, alive: true);
        }

        internal static void Sync(ExplosiveProjectile projectile, bool alive)
        {
            if (!Together.Net.IsServer) return;
            var writer = Together.Multiplayer.CreateNamedWriter(MessageId);
            writer.Put(projectile.NetworkId);
            writer.Put(alive);
            writer.Put(projectile.Position.x);
            writer.Put(projectile.Position.y);
            writer.Put(projectile.Velocity.x);
            writer.Put(projectile.Velocity.y);
            Together.Net.Server_SendToClients(LiteNetLib.DeliveryMethod.ReliableOrdered, in writer,
                Together.ServerMain.AllClientIdsExceptHost);
        }

        internal static void Receive(LiteNetLib.Utils.NetDataReader reader)
        {
            reader.Get(out int id);
            reader.Get(out bool alive);
            reader.Get(out float x);
            reader.Get(out float y);
            reader.Get(out float vx);
            reader.Get(out float vy);
            replicas.TryGetValue(id, out var projectile);
            if (!alive)
            {
                if (projectile != null) Object.Destroy(projectile.gameObject);
                replicas.Remove(id);
                return;
            }
            if (projectile == null)
            {
                projectile = ExplosiveProjectile.Create(new Vector2(x, y), new Vector2(vx, vy));
                projectile.NetworkId = id;
                replicas[id] = projectile;
            }
            projectile.Correct(new Vector2(x, y), new Vector2(vx, vy));
        }

        internal static void Forget(int id, ExplosiveProjectile projectile)
        {
            if (replicas.TryGetValue(id, out var current) && ReferenceEquals(current, projectile))
                replicas.Remove(id);
        }
    }
}
