using UnityEngine;
using UnityEngine.Advertisements;

public class AdManager : MonoBehaviour
{
  public void ShowAd()
  {
    if (Advertisement.IsReady())
    {
      Advertisement.Show();
    }
  }
}
