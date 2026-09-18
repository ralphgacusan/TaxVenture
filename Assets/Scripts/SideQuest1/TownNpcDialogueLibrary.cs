using System.Collections.Generic;

/// <summary>
/// PURPOSE:
/// Hardcoded dialogue for the 4 Level 1 town NPCs. Uses your existing
/// DialogueBuilder / DialogueLine data model, so it plugs straight into
/// DialogueUI.StartDialogue() with no changes to that system.
///
/// Player lines below are Elijah's (the tax consultant / player
/// character). Edit the strings below to change what each NPC says.
/// TaxpayerNpcInteractable just calls GetDialogueFor(npcId, ...).
/// </summary>
public static class TownNpcDialogueLibrary
{
    public static List<DialogueLine> GetDialogueFor(string npcId, string npcDisplayName)
    {
        var builder = new DialogueBuilder(npcDisplayName);

        switch (npcId)
        {
            // Client 1 — Resident Citizen — Marie Bautista
            case "npc_marie":
                builder
                    .Player("Good afternoon. Mind if I ask you a few questions? I'm the new tax consultant assigned to the town.")
                    .Npc("Oh, sure. Is there something I need to prepare?")
                    .Player("Nothing serious. I'm getting familiar with the taxpayers around here. What do you do for a living?")
                    .Npc("I work at the BPO office nearby. I've been there for a few years now.")
                    .Player("I see. And have you been living here in the Philippines for most of your life?")
                    .Npc("Yes. I was born here, actually. My family and I have always lived here.")
                    .Player("And you're a Filipino citizen?")
                    .Npc("Yes.")
                    .Player("Got it. That gives me a better idea of your taxpayer status. Thanks for your time.")
                    .Npc("No problem. Good luck with your new job!");
                break;

            // Client 2 — Non-Resident Citizen — Jhun-jhun Templo
            case "npc_jhunjhun":
                builder
                    .Player("Excuse me. Are you waiting for someone?")
                    .Npc("Actually, I'm waiting for my ride. I'm heading back to the airport tomorrow.")
                    .Player("Ah, traveling?")
                    .Npc("Yeah. I work in Qatar. I'm just here visiting my family.")
                    .Player("I see. How long have you been working there?")
                    .Npc("Almost five years now. I usually only come home for a few weeks.")
                    .Player("And you're still a Filipino citizen?")
                    .Npc("Of course.")
                    .Player("So most of your time is spent living and working abroad?")
                    .Npc("Yes. My job, apartment, and most of my life are there.")
                    .Player("That distinction is important for your tax situation. Thanks, Jhun-jhun. I hope you have a safe trip back.")
                    .Npc("Thanks. And good luck with the consulting job!");
                break;

            // Client 3 — Resident Alien — "Milo Zenn"
            case "npc_milo":
                builder
                    .Player("Good afternoon. Is this your restaurant?")
                    .Npc("Yes. I've been running it for a few years now.")
                    .Player("Nice. I'm Elijah, the new tax consultant assigned to the town.")
                    .Npc("Ah, good to meet you.")
                    .Player("I noticed you have quite a few customers. Have you been living here long?")
                    .Npc("About six years now.")
                    .Player("Six years? So you've settled here?")
                    .Npc("Pretty much. I have a house nearby, and my family lives here too.")
                    .Player("That's interesting. Are you a Filipino citizen?")
                    .Npc("No. I'm from... somewhere a little farther away.")
                    .Player("Farther away?")
                    .Npc("Very far away.")
                    .Player("...Right.")
                    .Npc("Anyway, I've been residing here for years.")
                    .Player("Then your residency and citizenship are both important when determining your tax classification.")
                    .Npc("That's what I was hoping you could help me with.")
                    .Player("That's the job.");
                break;

            // Client 4 — Non-Resident Alien — "Zorbix"
            case "npc_zorbix":
                builder
                    .Player("Good afternoon. Can I ask what brings you to town?")
                    .Npc("BUSINESS.")
                    .Player("...Right. Business.")
                    .Npc("I HAVE BEEN SENT TO THIS PLANET FOR A TEMPORARY COMMERCIAL PROJECT.")
                    .Player("Okay. And how long are you staying?")
                    .Npc("TWENTY-SEVEN DAYS.")
                    .Player("Do you normally live here?")
                    .Npc("NO. MY HOME IS VERY FAR FROM THIS PLANET.")
                    .Player("I figured.")
                    .Npc("I WILL RETURN HOME AFTER COMPLETING MY ASSIGNMENT.")
                    .Player("And are you receiving income for the work you're performing here?")
                    .Npc("YES.")
                    .Player("Then we'll need to determine how your Philippine-source income is treated.")
                    .Npc("THIS TAX SYSTEM IS MORE COMPLICATED THAN ANTICIPATED.")
                    .Player("Welcome to the Philippines.");
                break;

            default:
                builder.Npc("...");
                break;
        }

        return builder.Build();
    }
}