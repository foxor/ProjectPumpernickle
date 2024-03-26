package Pumpernickel;

import basicmod.BasicMod;

import com.evacipated.cardcrawl.modthespire.lib.LineFinder;
import com.evacipated.cardcrawl.modthespire.lib.SpireInsertPatch;
import com.evacipated.cardcrawl.modthespire.lib.SpirePatch;
import com.evacipated.cardcrawl.modthespire.lib.SpirePostfixPatch;
import com.evacipated.cardcrawl.modthespire.lib.SpirePrefixPatch;
import com.megacrit.cardcrawl.cards.AbstractCard;
import com.megacrit.cardcrawl.cards.CardGroup;
import com.megacrit.cardcrawl.cards.CardSave;
import com.megacrit.cardcrawl.cards.blue.Loop;
import com.megacrit.cardcrawl.cards.green.Alchemize;
import com.megacrit.cardcrawl.cards.green.Eviscerate;
import com.megacrit.cardcrawl.cards.purple.Blasphemy;
import com.megacrit.cardcrawl.cards.red.Anger;
import com.megacrit.cardcrawl.characters.AbstractPlayer;
import com.megacrit.cardcrawl.core.CardCrawlGame;
import com.megacrit.cardcrawl.core.Settings;
import com.megacrit.cardcrawl.dungeons.AbstractDungeon;
import com.megacrit.cardcrawl.dungeons.Exordium;
import com.megacrit.cardcrawl.dungeons.TheBeyond;
import com.megacrit.cardcrawl.dungeons.TheCity;
import com.megacrit.cardcrawl.events.beyond.MindBloom;
import com.megacrit.cardcrawl.events.city.TheLibrary;
import com.megacrit.cardcrawl.events.exordium.ScrapOoze;
import com.megacrit.cardcrawl.events.exordium.ShiningLight;
import com.megacrit.cardcrawl.events.shrines.Designer;
import com.megacrit.cardcrawl.helpers.CardHelper;
import com.megacrit.cardcrawl.map.MapRoomNode;
import com.megacrit.cardcrawl.monsters.beyond.Nemesis;
import com.megacrit.cardcrawl.monsters.beyond.TimeEater;
import com.megacrit.cardcrawl.monsters.exordium.Cultist;
import com.megacrit.cardcrawl.relics.AbstractRelic;
import com.megacrit.cardcrawl.relics.ClockworkSouvenir;
import com.megacrit.cardcrawl.relics.Duality;
import com.megacrit.cardcrawl.relics.IncenseBurner;
import com.megacrit.cardcrawl.relics.MutagenicStrength;
import com.megacrit.cardcrawl.relics.OrangePellets;
import com.megacrit.cardcrawl.relics.SneckoEye;
import com.megacrit.cardcrawl.relics.TungstenRod;
import com.megacrit.cardcrawl.rewards.RewardItem;
import com.megacrit.cardcrawl.rooms.AbstractRoom;
import com.megacrit.cardcrawl.rooms.AbstractRoom.RoomPhase;
import com.megacrit.cardcrawl.rooms.EventRoom;
import com.megacrit.cardcrawl.rooms.MonsterRoom;
import com.megacrit.cardcrawl.rooms.MonsterRoomElite;
import com.megacrit.cardcrawl.rooms.ShopRoom;
import com.megacrit.cardcrawl.rooms.TreasureRoom;
import com.megacrit.cardcrawl.saveAndContinue.SaveFile;
import com.megacrit.cardcrawl.screens.CardRewardScreen;
import com.megacrit.cardcrawl.screens.CombatRewardScreen;
import com.megacrit.cardcrawl.screens.select.BossRelicSelectScreen;
import com.megacrit.cardcrawl.screens.select.GridCardSelectScreen;
import com.megacrit.cardcrawl.shop.ShopScreen;
import com.megacrit.cardcrawl.shop.StorePotion;
import com.megacrit.cardcrawl.shop.StoreRelic;

import basemod.abstracts.events.phases.CombatPhase;

import com.megacrit.cardcrawl.neow.NeowEvent;
import com.megacrit.cardcrawl.neow.NeowReward;
import com.megacrit.cardcrawl.neow.NeowRoom;
import com.megacrit.cardcrawl.orbs.Lightning;
import com.megacrit.cardcrawl.powers.ArtifactPower;
import com.megacrit.cardcrawl.powers.ConfusionPower;
import com.megacrit.cardcrawl.powers.FireBreathingPower;
import com.megacrit.cardcrawl.powers.IntangiblePower;

import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;

import java.io.*;
import java.lang.reflect.Array;
import java.lang.reflect.Field;
import java.net.Socket;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;

public class SpirePatches {
    public static final Logger logger = LogManager.getLogger(BasicMod.modID); //Used to output to the console.
    private static void cardReward() {
    	PumpernickelMessage message = new PumpernickelMessage();
        message.AddLine("Reward");
        message.AddLine(AbstractDungeon.floorNum);

        if (AbstractDungeon.getCurrRoom().getClass() == TreasureRoom.class) {
            message.AddLine("false");
        }
        else {
            message.AddLine("true"); // Are we expecting a fight to have just ended
        }
        boolean alreadySaidGreenKey = false;
        for (RewardItem rewardItem : AbstractDungeon.combatRewardScreen.rewards) {
            switch (rewardItem.type) {
                case CARD: {
                    message.AddLine("Cards");
                    for (AbstractCard card : rewardItem.cards) {
                        if (card.upgraded) {
                            message.AddLine(card.cardID + "+");
                        }
                        else {
                            message.AddLine(card.cardID);
                        }
                    }
                    break;
                }
                case STOLEN_GOLD:
                case GOLD: {
                    message.AddLine("Gold");
                    message.AddLine(rewardItem.goldAmt);
                    break;
                }
                case POTION: {
                    message.AddLine("Potion");
                    message.AddLine(rewardItem.potion.ID);
                    break;
                }
                case RELIC: {
                    message.AddLine("Relic");
                    message.AddLine(rewardItem.relic.relicId);
                }
                case EMERALD_KEY: {
                    // No idea why this is listed as a rewardItem twice
                	boolean keyIsHere = AbstractDungeon.getCurrMapNode().hasEmeraldKey;
                    if (!alreadySaidGreenKey && !Settings.hasEmeraldKey && keyIsHere) {
                        message.AddLine("Key");
                        message.AddLine("Green");
                        alreadySaidGreenKey = true;
                    }
                }
            }
        }
        message.Send();
    }
    public static void emeraldSet() {
    	for(ArrayList< MapRoomNode > row : AbstractDungeon.map) {
	        for(MapRoomNode node : row) {
	            if (node.hasEmeraldKey) {
	            	PumpernickelMessage message = new PumpernickelMessage();
                    message.AddLine("GreenKey");
                    message.AddLine(AbstractDungeon.actNum);
                    message.AddLine(node.x);
                    message.AddLine(node.y);
                    message.Send();
	            }
	        }
	    }
    }
    public static void roomTransition(SaveFile savefile) {
    	try {
	        AbstractRoom room = AbstractDungeon.getCurrRoom();
	        if (room instanceof EventRoom) {
	            EventRoom event = (EventRoom) room;
	        	PumpernickelMessage message = new PumpernickelMessage();
	            message.AddLine("Event");
	            message.AddLine(AbstractDungeon.floorNum + "");
	            message.AddLine(event.event.getClass().getName());
	            if (Designer.class.isAssignableFrom(event.event.getClass())) {
	                Designer d = (Designer)event.event;
	                
	                Field adjust = Designer.class.getDeclaredField("adjustmentUpgradesOne");
	                adjust.setAccessible(true);
	                message.AddLine(((boolean)adjust.get(d)) ? "True\n" : "False");
	
	                Field cleanup = Designer.class.getDeclaredField("cleanUpRemovesCards");
	                cleanup.setAccessible(true);
	                message.AddLine(((boolean)cleanup.get(d)) ? "True\n" : "False");
	            }
	            message.Send();
	        }
	        else if (room instanceof MonsterRoom) {
	        	if (!savefile.post_combat) {
		        	PumpernickelMessage message = new PumpernickelMessage();
		            message.AddLine("Fight");
		            message.AddLine(AbstractDungeon.floorNum);
		            message.AddLine(AbstractDungeon.lastCombatMetricKey);
		            message.Send();
	        	}
	        }
    	}
    	catch(Exception e) {
    	}
    }
    public static void bossRelicReward() {
    	PumpernickelMessage message = new PumpernickelMessage();
        message.AddLine("Reward");
        message.AddLine(AbstractDungeon.floorNum + "");
        message.AddLine("false"); // Are we expecting a fight to have just ended
        message.AddLine("Relic");
        AbstractRoom room = AbstractDungeon.getCurrRoom();
        for (AbstractRelic relic : AbstractDungeon.bossRelicScreen.relics) {
            message.AddLine(relic.relicId);
        }
        message.Send();
    }
    public static void newDungeon() {
    	PumpernickelMessage message = new PumpernickelMessage();
        message.AddLine("NewDungeon");
        message.AddLine("Act: " + AbstractDungeon.actNum + "");
        message.AddLine("CurrentHealth: " + AbstractDungeon.player.currentHealth + "");
        message.AddLine("FirstBoss: " + AbstractDungeon.bossKey + "");
        message.AddLine("Cards: ");
        for (CardSave card : AbstractDungeon.player.masterDeck.getCardDeck()) {
            message.AddLine(card.id + ":" + card.upgrades + "");
        }
        message.AddLine("Relics: ");
        for (AbstractRelic relic : AbstractDungeon.player.relics) {
            message.AddLine(relic.relicId + "");
        }
        for(ArrayList< MapRoomNode > row : AbstractDungeon.map) {
            for(MapRoomNode node : row) {
                if (node.hasEmeraldKey) {
                    message.AddLine("GreenKey");
                    message.AddLine(AbstractDungeon.actNum);
                    message.AddLine(node.x + "");
                    message.AddLine(node.y + "");
                }
            }
        }
        message.Send();
    }
    public static void shop() {
    	try {
	    	PumpernickelMessage message = new PumpernickelMessage();
	        message.AddLine("Shop");
	        message.AddLine(AbstractDungeon.floorNum + "");
	        message.AddLine("Cards");
	        for (AbstractCard card : AbstractDungeon.shopScreen.coloredCards) {
	            message.AddLine(card.cardID + ": " + card.price + "");
	        }
	        for (AbstractCard card : AbstractDungeon.shopScreen.colorlessCards) {
	            message.AddLine(card.cardID + ": " + card.price + "");
	        }
	        message.AddLine("Relics");
	        Field relicField = ShopScreen.class.getDeclaredField("relics");
	        relicField.setAccessible(true);
	        ArrayList<StoreRelic> relics = (ArrayList<StoreRelic>)relicField.get(AbstractDungeon.shopScreen);
	        for (StoreRelic relic : relics) {
	            message.AddLine(relic.relic.relicId + ": " + relic.price + "");
	        }
	        message.AddLine("Potions");
	        Field potionField = ShopScreen.class.getDeclaredField("potions");
	        potionField.setAccessible(true);
	        ArrayList<StorePotion> potions = (ArrayList<StorePotion>)potionField.get(AbstractDungeon.shopScreen);
	        for (StorePotion potion : potions) {
	            message.AddLine(potion.potion.ID + ": " + potion.price + "");
	        }
	        message.Send();
    	}
    	catch (Exception e) {
    	}
    }
    public static void neow() {
    	try {
	    	PumpernickelMessage message = new PumpernickelMessage();
	        message.AddLine("Neow");
	        message.AddLine(Settings.seed + "");
	        NeowEvent event = (NeowEvent)AbstractDungeon.getCurrRoom().event;
	        
	        Field rewardField = NeowEvent.class.getDeclaredField("rewards");
	        rewardField.setAccessible(true);
	        ArrayList<NeowReward> rewards = (ArrayList<NeowReward>)rewardField.get(event);
	        for (NeowReward reward : rewards) {
	            message.AddLine(reward.drawback.name() + ": " + reward.type.name() + "");
	        }
	        message.Send();
		}
		catch (Exception e) {
		}
    }
    public static void openGrid() {
    	if (AbstractDungeon.gridSelectScreen.forPurge) {
    		return;
    	}
    	if (AbstractDungeon.gridSelectScreen.forUpgrade) {
    		return;
    	}
    	if (AbstractDungeon.gridSelectScreen.forTransform) {
    		return;
    	}
    	PumpernickelMessage message = new PumpernickelMessage();
        message.AddLine("Reward");
        message.AddLine(AbstractDungeon.floorNum + "");
        message.AddLine("false");
        message.AddLine("Cards");
        for (AbstractCard card : AbstractDungeon.gridSelectScreen.targetGroup.group) {
            if (card.upgraded) {
                message.AddLine(card.cardID + "+");
            }
            else {
                message.AddLine(card.cardID + "");
            }
        }
        message.Send();
    }
    public static void checkForUnexpectedReward() {
        AbstractRoom room = AbstractDungeon.getCurrRoom();
        if (!(room instanceof MonsterRoom)) {
        	PumpernickelMessage message = new PumpernickelMessage();
            message.AddLine("Reward");
            message.AddLine(AbstractDungeon.floorNum + "");
            message.AddLine("false");
            message.AddLine("Cards");

            for (AbstractCard card : AbstractDungeon.cardRewardScreen.rewardGroup) {
                if (card.upgraded) {
                    message.AddLine(card.cardID + "+");
                }
                else {
                    message.AddLine(card.cardID);
                }
            }
            message.Send();
        }
    }
    public static void oozeClick() {
    	PumpernickelMessage message = new PumpernickelMessage();
        message.AddLine("Ooze");
        message.Send();
    }
    public static void obtainRelic(AbstractRelic relic) {
    	PumpernickelMessage message = new PumpernickelMessage();
        message.AddLine("Reward");
        message.AddLine(AbstractDungeon.floorNum + "");
        message.AddLine("false"); // Are we expecting a fight to have just ended
        message.AddLine("Relic");
        message.AddLine(relic.relicId + "");
        message.Send();
    }
    public static void genericUpdate() {
    	PumpernickelMessage message = new PumpernickelMessage();
        message.AddLine("Reward");
        message.AddLine(AbstractDungeon.floorNum + "");
        message.AddLine("false");
        message.Send();
    }
    // Recompute after generating a card reward
    @SpirePatch( clz = CombatRewardScreen.class, method = "setupItemReward" )
    public static class AbstractDungeonGetRewardCardsPatch {
        @SpirePostfixPatch public static void Postfix() { cardReward(); }
    }

    @SpirePatch( clz = AbstractDungeon.class, method = "setEmeraldElite")
    public static class AbstractDungeonSetEmeraldElitePatch {
        @SpirePostfixPatch public static void Postfix() { emeraldSet(); }
    }
    @SpirePatch(clz = AbstractDungeon.class, method = "nextRoomTransition", paramtypez = { SaveFile.class })
    public static class OnEventRolledPatch {
        @SpirePostfixPatch public static void Postfix(AbstractDungeon __instance, SaveFile savefile) { roomTransition(savefile); }
    }
    @SpirePatch(clz = BossRelicSelectScreen.class, method = "open", paramtypez = { ArrayList.class })
    public static class OnBossRelicPatch {
        @SpirePostfixPatch public static void Postfix() { bossRelicReward(); }
    }
    @SpirePatch(clz = ShopScreen.class, method = "init", paramtypez = { ArrayList.class, ArrayList.class })
    public static class OnEnterShopPatch {
        @SpirePostfixPatch public static void Postfix() { shop(); }
    }
    @SpirePatch(clz = NeowEvent.class, method = "blessing", paramtypez = { })
    public static class OnNeowRewardPatch {
        @SpirePostfixPatch public static void Postfix() { neow(); }
    }
    @SpirePatch(clz = GridCardSelectScreen.class, method = "open", paramtypez = { CardGroup.class, int.class, String.class, boolean.class, boolean.class, boolean.class, boolean.class})
    public static class OnLibraryPresentPatch {
        @SpirePostfixPatch public static void Postfix() { openGrid(); }
    }
    @SpirePatch(clz = CardRewardScreen.class, method = "open", paramtypez = { ArrayList.class, RewardItem.class, String.class})
    public static class OnCardRewardPatch {
    	@SpirePostfixPatch public static void Postfix() { checkForUnexpectedReward(); }
	}
	@SpirePatch(clz = ScrapOoze.class, method = "buttonEffect", paramtypez = { int.class })
	public static class OnOozeClickPatch {
		@SpirePrefixPatch public static void Postfix() { oozeClick(); }
	}
	@SpirePatch(clz = AbstractRoom.class, method = "spawnRelicAndObtain", paramtypez = { float.class, float.class, AbstractRelic.class })
	public static class OnGainRelicPatch {
		@SpirePrefixPatch public static void Postfix(AbstractRoom __instance, float x, float y, AbstractRelic relic) { obtainRelic(relic); }
	}
	@SpirePatch(clz = CardHelper.class, method = "obtain", paramtypez = { String.class, AbstractCard.CardRarity.class, AbstractCard.CardColor.class })
	public static class OnCardAddedPatch {
		@SpirePostfixPatch public static void Postfix() { genericUpdate(); }
	}
	@SpirePatch(clz = ShiningLight.class, method = "upgradeCards", paramtypez = { })
	public static class OnShiningLightUpgradePatch {
		@SpirePostfixPatch public static void Postfix() { genericUpdate(); }
	}
}
