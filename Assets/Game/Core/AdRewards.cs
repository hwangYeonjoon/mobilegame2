using System;
namespace ScrapGuard.Core
{
    [Serializable] public class AdRewardStatus { public bool enabled; public string mode, unitId; }
    [Serializable] public class AdClaim
    {
        public string id, placement, status, userId, unitId, expires;
        public int gold, parts;
        public bool test;
        public ServerReply reply;
    }
}
