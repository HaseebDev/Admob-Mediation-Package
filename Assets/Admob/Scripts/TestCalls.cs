using UnityEngine;
using System;
public class TestCalls : MonoBehaviour
{
    Action Sucess, Fail;




    public void CallInterstital(int number)
    {

        switch (number)
        {
            case 0:
                Sucess = OnSucessInter;
                AdsManager.Instance.ShowInterstitial(Sucess);
                break;
            case 1:
                AdsManager.Instance.ShowInterstitial();
                break;

            case 2:
                Sucess = OnSucessInter;
                Fail = OnFailInter;
                AdsManager.Instance.ShowInterstitial(Sucess, Fail);
                break;
        }
    }

    public void CallRewarded(int number)
    {
        switch (number)
        {
            case 0:
                Sucess = OnSucessRewarded;
                AdsManager.Instance.ShowRewarded(Sucess);
                break;
            case 1:
                AdsManager.Instance.ShowRewarded();
                break;

            case 2:
                Sucess = OnSucessRewarded;
                Fail = OnFailRewarded;
                AdsManager.Instance.ShowRewarded(Sucess, Fail);
                break;
        }
    }


    private void OnSucessInter()
    {
        Debug.Log($" Intestitial Called with Sucess");
    }
    private void OnFailInter()
    {
        Debug.Log($" Intestitial Called with Failure");
    }

    private void OnSucessRewarded()
    {
        Debug.Log($" Rewarded Called with Sucess");
    }
    private void OnFailRewarded()
    {
        Debug.Log($" Rewarded Called with Failure");
    }


    public void ShowBannerTestCall()
    {
        AdsManager.Instance.ShowBanner(true);
    }

}
