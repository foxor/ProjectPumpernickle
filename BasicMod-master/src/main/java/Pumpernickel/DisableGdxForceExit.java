package Pumpernickel;

import com.badlogic.gdx.Gdx;
import com.badlogic.gdx.backends.lwjgl.LwjglApplication;
import com.badlogic.gdx.backends.lwjgl.LwjglApplicationConfiguration;
import com.badlogic.gdx.backends.lwjgl.LwjglGraphics;
import com.evacipated.cardcrawl.modthespire.Loader;
import com.evacipated.cardcrawl.modthespire.lib.SpireInsertPatch;
import com.evacipated.cardcrawl.modthespire.lib.SpirePatch;
import com.megacrit.cardcrawl.core.CardCrawlGame;
import com.megacrit.cardcrawl.saveAndContinue.SaveAndContinue;
import com.megacrit.cardcrawl.saveAndContinue.SaveFile;
import com.megacrit.cardcrawl.saveAndContinue.SaveFile.SaveType;

import java.io.Console;
import java.lang.reflect.Field;

@SpirePatch(
	clz = CardCrawlGame.class, method = "update"
)
public class DisableGdxForceExit
{
    @SpireInsertPatch(loc=868)
    public static void Insert(CardCrawlGame __instance)
    {
    	SpirePatches.newDungeon();
    }
}