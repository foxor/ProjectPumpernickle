package Pumpernickel;

import basemod.BaseMod;
import basicmod.BasicMod;
import com.evacipated.cardcrawl.modthespire.lib.SpirePatch;
import com.evacipated.cardcrawl.modthespire.lib.SpirePostfixPatch;
import com.megacrit.cardcrawl.cards.AbstractCard;
import com.megacrit.cardcrawl.cards.CardSave;
import com.megacrit.cardcrawl.characters.AbstractPlayer;
import com.megacrit.cardcrawl.core.CardCrawlGame;
import com.megacrit.cardcrawl.dungeons.AbstractDungeon;
import com.megacrit.cardcrawl.helpers.EventHelper;
import com.megacrit.cardcrawl.map.MapRoomNode;
import com.megacrit.cardcrawl.relics.AbstractRelic;
import com.megacrit.cardcrawl.rewards.RewardItem;
import com.megacrit.cardcrawl.screens.CombatRewardScreen;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;

import java.io.*;
import java.net.ServerSocket;
import java.net.Socket;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;

public class SpirePatches {
    public static final Logger logger = LogManager.getLogger(BasicMod.modID); //Used to output to the console.
    private static void cardReward() {
        try {
            Socket clientSocket = new Socket("localhost",13076);
            DataOutputStream outToServer =
                    new DataOutputStream(clientSocket.getOutputStream());
            outToServer.write("Reward\n".getBytes(StandardCharsets.UTF_8));
            for (RewardItem rewardItem : AbstractDungeon.combatRewardScreen.rewards) {
                switch (rewardItem.type) {
                    case CARD: {
                        outToServer.write("Cards\n".getBytes(StandardCharsets.UTF_8));
                        for (AbstractCard card : rewardItem.cards) {
                            if (card.upgraded) {
                                outToServer.write((card.cardID + "+\n").getBytes(StandardCharsets.UTF_8));
                            }
                            else {
                                outToServer.write((card.cardID + "\n").getBytes(StandardCharsets.UTF_8));
                            }
                        }
                        break;
                    }
                    case GOLD: {
                        outToServer.write("Gold\n".getBytes(StandardCharsets.UTF_8));
                        break;
                    }
                    case POTION: {
                        outToServer.write("Potion\n".getBytes(StandardCharsets.UTF_8));
                        outToServer.write((rewardItem.potion.ID + "\n").getBytes(StandardCharsets.UTF_8));
                        break;
                    }
                }
            }
            outToServer.write("Done\n".getBytes(StandardCharsets.UTF_8));
            outToServer.flush();
            clientSocket.close();
        }
        catch (Exception e) {
        }
    }
    public static void emeraldSet() {
        try {
            Socket clientSocket = new Socket("localhost",13076);
            DataOutputStream outToServer =
                    new DataOutputStream(clientSocket.getOutputStream());
            outToServer.write("GreenKey\n".getBytes(StandardCharsets.UTF_8));
            for(ArrayList< MapRoomNode > row : AbstractDungeon.map) {
                for(MapRoomNode node : row) {
                    if (node.hasEmeraldKey) {
                        outToServer.write((node.x + "\n").getBytes(StandardCharsets.UTF_8));
                        outToServer.write((node.y + "\n").getBytes(StandardCharsets.UTF_8));
                    }
                }
            }
            outToServer.write("Done\n".getBytes(StandardCharsets.UTF_8));
            outToServer.flush();
            clientSocket.close();
            EventHelper
        }
        catch (Exception e) {
        }
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
}