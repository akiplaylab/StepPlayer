using UnityEngine;

public sealed class ResultSceneController : MonoBehaviour
{
    [SerializeField] ResultPresenter resultPresenter;

    void Start()
    {
        if (resultPresenter != null)
        {
            resultPresenter.Show(ResultData.Summary);
        }
    }
}
