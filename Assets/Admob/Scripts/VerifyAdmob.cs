using UnityEngine;

public class VerifyAdmob : MonoBehaviour
{
    [SerializeField] private BannerPosition bannerPosition = BannerPosition.Bottom;
    [SerializeField] private bool showBannerOnStart = true;
    void Start()
    {
        AdsManager.Instance.VerifyHit();
        AdsManager.Instance.SetBannerPosition(bannerPosition);
        if (showBannerOnStart)
        {
            AdsManager.Instance.ShowBanner(true);
        }

    }



}
public enum BannerPosition
{
    Top,
    Bottom,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
    Center
}