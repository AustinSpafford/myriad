using UnityEngine;
using System.Collections;
using System.Linq;

public class ActiveOnPlatform : MonoBehaviour
{
	public RuntimePlatform[] WhitelistedPlatforms = null;
	public RuntimePlatform[] BlacklistedPlatforms = null;

	public void Awake()
	{
		bool passesWhitelist = (
			(WhitelistedPlatforms.Any() == false) ||
			WhitelistedPlatforms.Contains(Application.platform));
		
		bool passesBlacklist = 
			(BlacklistedPlatforms.Contains(Application.platform) == false);

		bool shouldBeActive = (passesWhitelist && passesBlacklist);

		// NOTE: We intentionally avoid activating inactive objects, since there 
		// might be multiple activation-filters present.
		if (shouldBeActive == false)
		{
			this.gameObject.SetActive(false);
		}
	}
}