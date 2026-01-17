using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Titled_Gui.Data.Game;
using static Titled_Gui.Data.Entity.WorldEntityManager;

namespace Titled_Gui.Data.Entity
{
    public class WorldEntity
    {
        public nint pawnAddress {  get; set; }
        public Vector3 position { get; set; }
        public Vector2 position2D { get; set; }
        public IntPtr itemNode { get; set; }
        public EntityKind type {get; set; }
        public string displayName { get; set; } = "Unknown";
        public string GetSchemaName()
        {
            var identity = GameState.swed.ReadPointer(this.pawnAddress + Offsets.m_pEntity);

            return GameState.swed.ReadString(identity + Offsets.m_designerName, 32);
        }

    }
}
