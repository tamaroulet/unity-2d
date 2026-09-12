using System;

namespace Game.Features.Boss
{
    public class BossChargeState
    {
        public const float DefaultChargeSeconds = 0.2f;

        public BossChargeState() : this(DefaultChargeSeconds)
        {
        }

        public BossChargeState(float chargeSeconds)
        {
            if (float.IsNaN(chargeSeconds) || chargeSeconds < 0f)
            {
                chargeSeconds = 0f;
            }

            ChargeSeconds = chargeSeconds;
            Phase = BossChargePhase.Idle;
            ElapsedSeconds = 0f;
        }

        public float ChargeSeconds { get; }
        public BossChargePhase Phase { get; private set; }
        public float ElapsedSeconds { get; private set; }

        public void BeginCharge()
        {
            Phase = BossChargePhase.Charging;
            ElapsedSeconds = 0f;
        }

        public void Reset()
        {
            Phase = BossChargePhase.Idle;
            ElapsedSeconds = 0f;
        }

        public void Update(float deltaSeconds)
        {
            if (Phase != BossChargePhase.Charging)
            {
                return;
            }

            if (deltaSeconds > 0f && !float.IsNaN(deltaSeconds) && !float.IsInfinity(deltaSeconds))
            {
                ElapsedSeconds += deltaSeconds;
                if (float.IsInfinity(ElapsedSeconds))
                {
                    ElapsedSeconds = float.MaxValue;
                }
            }

            if (ElapsedSeconds >= ChargeSeconds)
            {
                Phase = BossChargePhase.Dashing;
            }
        }
    }
}
