namespace GunPowdered.Features.Grenades.Domain
{
    /// <summary>What the controls should be after one step: the next phase and, when Intercept is true, the values the game gets instead of the player's own.</summary>
    internal readonly struct AimStep
    {
        internal AimStep(AimPhase next, bool intercept, bool attack, bool suppressBlock, AimEvent happened)
        {
            Next = next;
            Intercept = intercept;
            Attack = attack;
            SuppressBlock = suppressBlock;
            Happened = happened;
        }

        internal AimPhase Next { get; }

        /// <summary>False: leave the player's controls untouched (a normal click, the normal block).</summary>
        internal bool Intercept { get; }

        /// <summary>The attack press the game gets (true for exactly one step: the throw). The attack-hold is always off while intercepting.</summary>
        internal bool Attack { get; }

        /// <summary>The game gets no block while aiming, so the right button cancels instead of blocking.</summary>
        internal bool SuppressBlock { get; }

        internal AimEvent Happened { get; }
    }
}
