using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;

namespace RespawnButton.Patches
{
    static class OverridePostRunDestinationPatch
    {
        public delegate void OverridePostRunDestinationDelegate(Run run);
        public static OverridePostRunDestinationDelegate PostRunDestinationOverride;

        [SystemInitializer]
        static void Init()
        {
            IL.RoR2.Run.OnDestroy += il =>
            {
                ILCursor c = new ILCursor(il);

                if (c.TryGotoNext(MoveType.After, x => x.MatchCallOrCallvirt<Run>(nameof(Run.HandlePostRunDestination))))
                {
                    ILLabel afterPostRunCallLabel = c.MarkLabel();

                    c.Index -= 2;

                    c.Emit(OpCodes.Ldarg_0);
                    c.EmitDelegate((Run run) =>
                    {
                        if (PostRunDestinationOverride is null)
                        {
                            return true;
                        }
                        else
                        {
                            PostRunDestinationOverride(run);
                            return false;
                        }
                    });
                    c.Emit(OpCodes.Brfalse, afterPostRunCallLabel);
                }
                else
                {
                    Log.Error("Failed to find patch location");
                }
            };
        }
    }
}
