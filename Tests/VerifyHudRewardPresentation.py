"""Guard campaign reward tiles, HUD coin flights and their delayed presentation.
Run: python Tests/VerifyHudRewardPresentation.py
"""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]


def source(path):
    return (ROOT / "Assets/Scripts" / path).read_text(encoding="utf-8-sig")


failures = []


def check(condition, message):
    print(f"{'PASS' if condition else 'FAIL'}: {message}")
    if not condition:
        failures.append(message)


theme = source("UI/HUD/HudTheme.cs")
progression = source("UI/HUD/HudProgressionPanel.cs")
feedback = source("UI/HUD/HudFeedback.cs")
reward_screen = source("UI/HUD/HudRewardScreen.cs")
office_hud = source("UI/HUD/OfficeHudRuntime.cs")
level_complete = source("UI/LevelCompleteUI.cs")
side_quest = source("SideQuest1/SideQuestCompleteUI.cs")

check("public static Sprite Coin" in theme and "CoinCoverage" in theme,
      "The HUD coin uses a distinct generated coin icon")
check("WalletLabel => walletIcon" in progression and "HudTheme.Coin" in progression,
      "The wallet flight target is the visible coin icon")
check("walletRow.gameObject.SetActive(showWallet)" in progression and
      "SetWalletTarget(manager.CurrentProgress.campaign.coins, animate)" in progression,
      "The campaign wallet shows and counts the saved balance")
check(re.search(r"private void Start\(\)[\s\S]*?RefreshFromGame\(animate: false\);", progression),
      "The initial HUD refresh does not animate an unsaved coin grant")
check("if (earned <= 0) return;" in reward_screen,
      "Zero and capped rewards do not create a +0 tile")
check("BuildRewardTiles(data, y)" in reward_screen and
      'BalanceLabel} {reward.Balance' in reward_screen and '"+{reward.Earned} earned"' in reward_screen,
      "Reward tiles distinguish earned amounts from the saved balance")
check("bool earnedCoins = coins.Applied && coins.Amount > 0;" in feedback and
      "fx.Burst(HudTheme.Coin, HudTheme.CoinGold" in feedback and
      "grant.Coins.Amount > 0" in feedback,
      "Only positive applied coin grants fly to the wallet")
check("grant.Trust.Applied && grant.Trust.Amount > 0" in feedback,
      "A capped trust grant cannot produce a false heart flight")
check("pendingCampaign.Add" in feedback and "if (pendingCampaign.Count > 0 && progression != null)" in feedback and
      "!host.IsShown" in feedback and "!HudRewardScreen.AnyOpen" in office_hud,
      "Campaign flights wait until the reward card is closed and the HUD is visible")
check("if (commit == PlayerProgressManager.CaseResultCommit.Committed)" in level_complete and
      "HudFeedback.AnnounceCampaign(default, manager.LastCaseCoins, null)" in level_complete,
      "Case coins fly only on their first successful commit")
check('data.RewardTile("Coins"' in level_complete and 'data.RewardTile("Public Trust"' in level_complete and
      'data.RewardTile("Coins"' in side_quest and 'data.RewardTile("Public Trust"' in side_quest,
      "Case, chapter and side-quest cards use reward tiles")
check("hours.OnTime" in level_complete and "BonusAwarded" not in level_complete,
      "Level results use the on-time state directly")

raise SystemExit(1 if failures else 0)
