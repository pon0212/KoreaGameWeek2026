package com.tastepassport.tts;

import android.content.Context;
import android.os.Bundle;
import android.speech.tts.TextToSpeech;
import android.speech.tts.UtteranceProgressListener;
import java.util.Locale;

public final class TastePassportTts {
    private static TextToSpeech engine;
    private static String queuedText;
    private static String queuedTag = "vi-VN";
    private static float queuedVolume = 0.78f;
    private static boolean ready;
    private static String status = "ready";

    private TastePassportTts() { }

    public static synchronized void speak(Context context, String languageTag, String text, float volume) {
        if (context == null || text == null || text.trim().isEmpty()) {
            status = "error";
            return;
        }
        queuedTag = languageTag == null ? "vi-VN" : languageTag;
        queuedText = text;
        queuedVolume = Math.max(0f, Math.min(1f, volume));
        if (engine == null) {
            status = "initializing";
            engine = new TextToSpeech(context.getApplicationContext(), status -> {
                synchronized (TastePassportTts.class) {
                    ready = status == TextToSpeech.SUCCESS;
                    if (ready) {
                        engine.setSpeechRate(0.92f);
                        speakQueued();
                    } else {
                        queuedText = null;
                        TastePassportTts.status = "unavailable";
                    }
                }
            });
        } else if (ready) {
            speakQueued();
        } else if ("unavailable".equals(status)) {
            queuedText = null;
        }
    }

    private static void speakQueued() {
        if (!ready || queuedText == null || engine == null) return;
        Locale locale = Locale.forLanguageTag(queuedTag);
        int availability = engine.setLanguage(locale);
        if (availability < 0) {
            Locale languageOnly = new Locale(locale.getLanguage());
            availability = engine.setLanguage(languageOnly);
        }
        if (availability < 0) {
            queuedText = null;
            status = "language_unavailable";
            return;
        }
        String text = queuedText;
        queuedText = null;
        Bundle parameters = new Bundle();
        parameters.putFloat(TextToSpeech.Engine.KEY_PARAM_VOLUME, queuedVolume);
        int result = engine.speak(text, TextToSpeech.QUEUE_FLUSH, parameters, "taste-passport-story");
        status = result == TextToSpeech.ERROR ? "unavailable" : "ready";
    }

    public static synchronized String getStatus() {
        return status;
    }

    public static synchronized void stop() {
        queuedText = null;
        if (engine != null) engine.stop();
        status = ready ? "ready" : status;
    }
}
