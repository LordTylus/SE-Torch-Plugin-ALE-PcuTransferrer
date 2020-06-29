using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;
using ALE_Core.Utils;
using Sandbox.Game;
using Sandbox.Game.Entities.Cube;
using ALE_Core.Cooldown;
using Sandbox.Game.World.Generator;
using VRage.Game;
using System.Reflection;

namespace ALE_GridManager.Commands {

    public class SpecialCommands : CommandModule {

        private static readonly FieldInfo SeedParamField = typeof(MyProceduralWorldGenerator).GetField("m_existingObjectsSeeds", BindingFlags.NonPublic | BindingFlags.Instance);

        [Command("seedcleanup all", "Super Secret command :-) Deletes generated seeds for asteroids so they respawn after a !voxels cleanup asteroids command")]
        [Permission(MyPromoteLevel.Admin)]
        public void SeedCleanup() {

            var generator = MySession.Static.GetComponent<MyProceduralWorldGenerator>();
            var seeds = SeedParamField.GetValue(generator) as HashSet<MyObjectSeedParams>;

            int count = seeds.Count;

            seeds.Clear();

            Context.Respond(count + " Seeds cleaned up! Restart recommended!");
        }
    }
}
