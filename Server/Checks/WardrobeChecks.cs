using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using ScrapGuard.Core;
using ScrapGuard.Server;
static class WardrobeChecks
{
    public static void Run(string folder,Action<bool,string> check)
    {
        string path=Path.Combine(folder,"wardrobe.db"),token=Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+','-').Replace('/','_');
        var repo=new GameRepository(path,()=>new DateTimeOffset(2026,11,3,4,0,0,TimeSpan.Zero));var account=repo.Register(token);
        var state=account.state;state.gems=1800;state.heroes[1].unlocked=state.heroes[3].unlocked=state.heroes[4].unlocked=true;
        state.ownedCosmetics.Add(Outfits.RailKey);state.selectedOutfit=2;state.heroOutfits=null;
        using(var db=new SqliteConnection("Data Source="+path)){db.Open();using var q=db.CreateCommand();q.CommandText="UPDATE accounts SET state=$state WHERE id=$id";q.Parameters.AddWithValue("$state",JsonSerializer.Serialize(state,GameRepository.Json));q.Parameters.AddWithValue("$id",account.playerId);q.ExecuteNonQuery();}
        ServerReply Apply(GameCommand c){c.revision=repo.Bootstrap(token).revision;return repo.Apply(token,c);}
        Apply(new GameCommand("outfit_target"){skin=3});check(repo.Bootstrap(token).state.outfitTarget==3 && repo.Bootstrap(token).state.gems==1800,"server saving goal never charges");
        var buy=new GameCommand("cosmetic_buy"){value=Outfits.Product(3)};Apply(buy);repo.Apply(token,buy);
        check(repo.Bootstrap(token).state.gems==1200 && repo.Bootstrap(token).state.outfitTarget==0,"new outfit retry charges once and fulfills target");
        Apply(new GameCommand("outfit_equip"){skin=3});Apply(new GameCommand("cosmetic_buy"){value=Outfits.Product(4)});Apply(new GameCommand("outfit_equip"){skin=4});
        repo=new GameRepository(path);state=repo.Bootstrap(token).state;
        check(Outfits.Active(state,1)==2 && Outfits.Active(state,3)==3 && Outfits.Active(state,4)==4 && state.gems==600,"server wardrobe migration and independent equips survive restart");
        Apply(new GameCommand("customize"){hero=4});Apply(new GameCommand("visibility"){enabled=true});
        check(repo.GetProfile(token,account.playerId).outfit==4,"new outfit appears in public profile");
        Apply(new GameCommand("outfit_clear"){hero=3});state=repo.Bootstrap(token).state;
        check(Outfits.Active(state,3)==0 && Outfits.Active(state,1)==2 && Outfits.Active(state,4)==4,"server clears only selected hero");
        bool rejected=false;try{Apply(new GameCommand("outfit_equip"){skin=999});}catch(ApiFault){rejected=true;}
        check(rejected && repo.Bootstrap(token).state.gems==600,"invalid wardrobe command cannot mutate wallet");
    }
}
