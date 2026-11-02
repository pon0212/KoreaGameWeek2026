package com.tastepassport.tts;

import android.speech.tts.TextToSpeech;
import android.speech.tts.UtteranceProgressListener;
import java.util.Locale;
import com.unity3d.player.UnityPlayer;

public final class TastePassportTts {
    private static TextToSpeech engine;
    private static String queuedText;
    private static String queuedTag = "vi-VN";
    private static boolean ready;

    private TastePassportTts() { }

    public static synchronized void speak(String languageTag, String text) {
        if (text == null || text.trim().isEmpty()) return;
        queuedTag = languageTag == null ? "vi-VN" : languageTag;
        queuedText = text;
        if (engine == null) {
            engine = new TextToSpeech(UnityPlayer.currentActivity.getApplicationContext(), status -> {
                synchronized (TastePassportTts.class) {
                    ready = status == TextToSpeech.SUCCESS;
                    if (ready) {
                        engine.setSpeechRate(0.92f);
                        speakQueued();
                    } else {
                        queuedText = null;
                    }
                }
            });
        } else if (ready) {
            speakQueued();
        }
    }

    private static void speakQueued() {
        if (!ready || queuedText == null || engine == null) return;
        Locale locale = Locale.forLanguageTag(queuedTag);
        int availability = engine.setLanguage(locale);
        if (availability == TextToSpeech.LANG_MISSING_DATA || availability == TextToSpeech.LANG_NOT_SUPPORTED) {
            Locale languageOnly = new Locale(locale.getLanguage());
            availability = engine.setLanguage(languageOnly);
        }
        if (availability == TextToSpeech.LANG_MISSING_DATA || availability == TextToSpeech.LANG_NOT_SUPPORTED) {
            queuedText = null;
            return;
        }
        String text = queuedText;
        queuedText = null;
        engine.speak(text, TextToSpeech.QUEUE_FLUSH, null, "taste-passport-story");
    }

    public static synchronized void stop() {
        queuedText = null;
        if (engine != null) engine.stop();
    }
}
