using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.Purchasing;
namespace ScrapGuard
{
    public partial class GameApp
    {
        StoreController playStore;
        string billingAccount;
        CommerceStatus billingStatus;
        bool billingConnecting,billingVerifying;
        readonly Queue<Order> purchaseQueue=new Queue<Order>();
        void ConnectPlayBilling()
        {
            if(!Online||!cloudReady){Toast("서버 계정부터 연결해 주세요.");return;}
            if(Application.platform!=RuntimePlatform.Android){Toast("Google Play 내부 테스트로 설치한 Android 앱에서 사용해 주세요.");return;}
            if(networkBusy||billingConnecting)return;
            StartCoroutine(LoadCommerceStatus());
        }
        IEnumerator LoadCommerceStatus()
        {
            SetNetworkBusy(true);CommerceStatus status=null;
            yield return cloud.Get("/v1/commerce",(body,code)=>{if(code==200)status=JsonUtility.FromJson<CommerceStatus>(body);});SetNetworkBusy(false);
            if(status==null||!(status.enabled||status.licenseTest)){Toast("현재 Play 결제를 이용할 수 없어요.");yield break;}
            if(playStore!=null&&billingAccount!=status.accountBinding){Toast("계정을 바꾼 경우 앱을 다시 시작해 주세요.");yield break;}
            billingAccount=status.accountBinding;billingStatus=status;
            if(playStore!=null){playStore.FetchPurchases();PlayStorePanel();yield break;}
            InitializePlayStore();
        }
        async void InitializePlayStore()
        {
            billingConnecting=true;
            try
            {
                playStore=UnityIAPServices.StoreController();
                playStore.OnPurchasePending+=QueuePurchase;
                playStore.OnPurchaseDeferred+=order=>Toast("결제 보류 중 · 완료 후 지급됩니다.");
                playStore.OnPurchaseFailed+=order=>Toast("구매가 완료되지 않았어요. 취소 또는 결제 상태를 확인해 주세요.");
                playStore.OnProductsFetchFailed+=failure=>{billingConnecting=false;Toast("Play 상품을 불러오지 못했어요. 상품 등록 상태를 확인해 주세요.");};
                playStore.OnPurchasesFetchFailed+=failure=>Toast("구매 복원 확인에 실패했어요. 다시 시도해 주세요.");
                playStore.OnProductsFetched+=products=>{billingConnecting=false;playStore.FetchPurchases();PlayStorePanel();};
                playStore.OnPurchasesFetched+=orders=>{foreach(var order in orders.ConfirmedOrders)QueuePurchase(order);foreach(var order in orders.PendingOrders)QueuePurchase(order);};
                await playStore.Connect();
                playStore.GooglePlayStoreExtendedService.SetObfuscatedAccountId(billingAccount);
                playStore.FetchProducts(CommerceCatalog.Offers.Select(o=>new ProductDefinition(o.Id,o.Consumable?ProductType.Consumable:ProductType.NonConsumable)).ToList());
            }
            catch(Exception){playStore=null;billingConnecting=false;Toast("Play 연결에 실패했어요. 내부 테스트 설치 여부를 확인해 주세요.");}
        }
        void PlayStorePanel()
        {
            if(playStore==null)return;
            var card=Dialog(billingStatus.licenseTest?"Play 라이선스 테스트":"보석 충전 · 구매 복원",billingStatus.licenseTest?"등록된 테스트 계정 전용":"Google Play 가격 확인 후 구매할 수 있어요.",860);
            Ui.Label(card,billingStatus.licenseTest?"Google 테스트 결제 수단인지 확인해 주세요.\n실제 청구 수단이 보이면 구매를 취소하세요.":"구매 내역은 서버 계정에 보관됩니다.\n판매가 중지되어도 기존 구매 복원은 가능합니다.",24,135,444,64,16,Ui.Gold);
            var products=playStore.GetProducts();
            for(int i=0;i<CommerceCatalog.Offers.Length;i++)
            {
                var offer=CommerceCatalog.Offers[i];var product=products.FirstOrDefault(p=>p.definition.id==offer.Id);
                string price=product==null?"상품 확인 중":product.metadata.localizedPriceString;
                Ui.Button(card,offer.Name+" · "+price,24,225+i*90,444,65,()=>{
                    if(!Online||!cloudReady||networkBusy||combat!=null||billingVerifying||cloud.Pending!=null){Toast("진행 중인 작전과 동기화를 먼저 마쳐 주세요.");return;}
                    StartCoroutine(ConfirmBillingAccount(product));
                },Ui.Raised,product!=null&&product.availableToPurchase&&billingStatus.salesEnabled&&billingStatus.saleProducts!=null&&Array.IndexOf(billingStatus.saleProducts,offer.Id)>=0,17);
            }
            Ui.Label(card,"지급은 서버 검증 후 확정됩니다.\n보류·실패에는 지급하지 않으며 복원 시 중복 지급하지 않아요.",24,716,444,67,14,Ui.Muted);
        }
        IEnumerator ConfirmBillingAccount(Product product)
        {
            SetNetworkBusy(true);CommerceStatus status=null;
            yield return cloud.Get("/v1/commerce",(body,code)=>{if(code==200)status=JsonUtility.FromJson<CommerceStatus>(body);});SetNetworkBusy(false);
            if(status==null||!(status.enabled||status.licenseTest)||status.accountBinding!=billingAccount||!status.salesEnabled||status.saleProducts==null||Array.IndexOf(status.saleProducts,product.definition.id)<0){Toast("계정 또는 판매 상태가 변경됐어요. 상점을 다시 열어 주세요.");yield break;}
            playStore.PurchaseProduct(product);
        }
        void QueuePurchase(Order order)
        {
            purchaseQueue.Enqueue(order);if(!billingVerifying)StartCoroutine(VerifyPlayQueue());
        }
        IEnumerator VerifyPlayQueue()
        {
            billingVerifying=true;
            while(purchaseQueue.Count>0)
            {
                if(networkBusy||cloud.Busy){yield return null;continue;}
                var order=purchaseQueue.Dequeue();var items=order.CartOrdered.Items();
                if(!Online||!cloudReady||items.Count!=1||items[0].Quantity!=1||order.Info.Google==null)continue;
                if(order.Info.Google.ObfuscatedAccountId!=billingAccount){Toast("다른 계정에 연결된 구매예요.");continue;}
                SetNetworkBusy(true);ServerReply reply=null;
                yield return cloud.VerifyPurchase(new PurchaseRequest{productId=items[0].Product.definition.id,purchaseToken=order.Info.Google.PurchaseToken},r=>reply=r);
                if(reply!=null&&reply.state!=null)ApplyRemote(reply);
                SetNetworkBusy(false);
                if(reply!=null&&reply.success)
                {
                    // Only confirm/consume after the durable server transaction; SDK redelivers unconfirmed orders.
                    if(order is PendingOrder pending)playStore.ConfirmPurchase(pending);
                    Track("play_purchase",items[0].Product.definition.id);RefreshWallet();
                }
                Toast(reply==null?"구매 검증 연결이 끊겼어요. 복원으로 다시 확인해 주세요.":reply.message);
            }
            billingVerifying=false;
        }
    }
}
