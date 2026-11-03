using System;
namespace ScrapGuard.Core
{
    public static class PremiumWallet
    {
        public static bool Spend(GameState s,int amount)
        {
            if(amount<0||s.gemDebt>0||s.gems<amount)return false;
            int free=Math.Max(0,s.gems-s.paidGems);
            s.paidGems-=Math.Max(0,amount-free);s.gems-=amount;return true;
        }
        public static int Grant(GameState s,int amount)
        {
            if(amount<=0)throw new ArgumentOutOfRangeException("amount");
            int recovery=Math.Min(s.gemDebt,amount);s.gemDebt-=recovery;
            int available=amount-recovery;
            s.gems=checked(s.gems+available);s.paidGems=checked(s.paidGems+available);return available;
        }
    }
    [Serializable] public class PurchaseRequest {public string productId,purchaseToken;}
    [Serializable] public class CommerceStatus {public bool licenseTest;public string accountBinding;}
}
