"""Cold Ember: toda a trilha e os efeitos sonoros, sintetizados (mesmo motor e timbres do Rebound).

Uso:  python Tools/gen_audio.py
Saida: Assets/Audio/*.wav

- Vozes: oscilador com lowpass que fecha ao longo da nota (como o sequenciador do Rebound).
- Ruido filtrado (passa-baixa / passa-banda / passa-alta) com varredura de frequencia.
- Uma "sala" curta (impulso de ruido) para os efeitos nao soarem secos.
- A trilha do jogo sai em tres camadas do mesmo tamanho (base, bateria, melodia) que o jogo
  mistura conforme a tensao sobe.
"""
import os
import wave

import numpy as np
from scipy.signal import butter, lfilter, sosfilt

SR = 32000
OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "Audio")
rng = np.random.default_rng(7)


def freq(midi):
    return 440.0 * 2 ** ((midi - 69) / 12)


# ------------------------------------------------------------------ primitivas
def osc(kind, f0, f1, dur):
    n = max(1, int(dur * SR))
    f = np.geomspace(max(f0, 1), max(f1, 1), n) if f0 != f1 else np.full(n, f0)
    ph = np.cumsum(f / SR) % 1.0
    if kind == "sine":
        return np.sin(2 * np.pi * ph)
    if kind == "square":
        return np.where(ph < 0.5, 1.0, -1.0)
    if kind == "triangle":
        return 4 * np.abs(ph - 0.5) - 1
    if kind == "sawtooth":
        return 2 * ph - 1
    raise ValueError(kind)


def sweep_filter(sig, kind, f0, f1, q=0.7, chunks=24):
    """Filtro com frequencia que desliza de f0 a f1 (aplicado em blocos)."""
    out = np.zeros_like(sig)
    n = len(sig)
    edges = np.linspace(0, n, chunks + 1).astype(int)
    zi = None
    for i in range(chunks):
        a, b = edges[i], edges[i + 1]
        if b <= a:
            continue
        fc = f0 * (f1 / f0) ** (i / max(1, chunks - 1)) if f0 != f1 else f0
        fc = min(max(fc, 30), SR / 2 * 0.95)
        if kind == "bandpass":
            bw = fc / max(q, 0.3)
            lo, hi = max(20, fc - bw / 2), min(SR / 2 * 0.95, fc + bw / 2)
            bb, aa = butter(2, [lo, hi], btype="band", fs=SR)
        else:
            bb, aa = butter(2, fc, btype="low" if kind == "lowpass" else "high", fs=SR)
        if zi is None or len(zi) != max(len(aa), len(bb)) - 1:
            zi = np.zeros(max(len(aa), len(bb)) - 1)
        out[a:b], zi = lfilter(bb, aa, sig[a:b], zi=zi)
    return out


def env(n, attack=0.004, curve="exp"):
    t = np.arange(n) / SR
    a = np.clip(t / max(attack, 1e-4), 0, 1)
    if curve == "exp":
        d = np.exp(-6.9 * t / (n / SR))  # cai ~60 dB ate o fim
    else:
        d = 1 - t / (n / SR)
    return a * d


def tone(kind, f0, dur, gain, f1=None, attack=0.004):
    s = osc(kind, f0, f1 or f0, dur)
    return s * env(len(s), attack) * gain


def noise(dur, gain, kind, f0, f1=None, q=0.8):
    n = max(1, int(dur * SR))
    s = sweep_filter(rng.uniform(-1, 1, n), kind, f0, f1 or f0, q)
    return s * env(n, 0.002) * gain


def voice(kind, f, dur, gain, cutoff):
    """Nota do sequenciador: lowpass que fecha de cutoff a 35% dele."""
    s = osc(kind, f, f, dur)
    s = sweep_filter(s, "lowpass", cutoff, max(120, cutoff * 0.35), chunks=8)
    return s * env(len(s), 0.006) * gain


def room(sig, amount=0.3, length=0.5):
    n = int(SR * length)
    imp = rng.uniform(-1, 1, n) * (1 - np.arange(n) / n) ** 3.2
    wet = np.convolve(sig, imp) * 0.05
    out = np.zeros(len(wet))
    out[: len(sig)] += sig
    return out + wet * amount


def mix(length, *parts):
    """parts: (inicio_em_segundos, sinal)"""
    out = np.zeros(int(length * SR))
    for start, s in parts:
        a = int(start * SR)
        b = min(len(out), a + len(s))
        if b > a:
            out[a:b] += s[: b - a]
    return out


def trim(sig):
    idx = np.nonzero(np.abs(sig) > 1e-4)[0]
    return sig[: idx[-1] + 1] if len(idx) else sig


def write(name, data, peak=0.85, normalize=True):
    os.makedirs(OUT, exist_ok=True)
    data = np.asarray(data, dtype=np.float64)
    if normalize:
        m = np.max(np.abs(data)) or 1
        data = data / m * peak
    data = np.clip(data, -1, 1)
    with wave.open(os.path.join(OUT, name), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes((data * 32767).astype(np.int16).tobytes())
    print(f"ok {name} {len(data) / SR:.2f}s")


# ------------------------------------------------------------------ trilha
_ = None
Dm, Bb, F, C, A, Gm = [62, 65, 69, 74], [58, 62, 65, 70], [60, 65, 69, 72], [60, 64, 67, 72], [57, 61, 64, 69], [58, 62, 67, 70]


def kick(t_len=0.22):
    s = osc("sine", 150, 42, 0.12)
    s = np.concatenate([s, osc("sine", 42, 40, t_len - 0.12)])
    return s * env(len(s), 0.001) * 0.42


def hiss(dur, gain, cutoff, kind):
    n = int(dur * SR)
    s = noise_buffer[:n].copy()
    bb, aa = butter(2, cutoff if kind != "bandpass" else [cutoff * 0.7, cutoff * 1.4],
                    btype={"highpass": "high", "lowpass": "low", "bandpass": "band"}[kind], fs=SR)
    return lfilter(bb, aa, s) * env(n, 0.001) * gain


noise_buffer = rng.uniform(-1, 1, SR)


def sequence(track, bars, layer):
    """Renderiza `bars` compassos de uma faixa; layer: 'base' | 'drums' | 'lead' | 'all'."""
    step = 60 / track["bpm"] / 4
    total = bars * 16 * step
    parts = []
    for i in range(bars * 16):
        s, bar = i % 16, (i // 16) % len(track["chords"])
        t = i * step
        chord, root = track["chords"][bar], track["roots"][bar]
        if layer in ("base", "all"):
            b = track["bass"][s]
            if b is not None:
                parts.append((t, voice("square", freq(root + b), step * 1.7, 0.13, 420)))
                parts.append((t, voice("triangle", freq(root + b), step * 1.9, 0.16, 900)))
            a = track["arp"][s]
            if a is not None:
                parts.append((t, voice("triangle", freq(chord[a]), step * 1.3, 0.075, 2600)))
                parts.append((t, voice("square", freq(chord[a] + 12), step * 0.7, 0.014, 3200)))
        if layer in ("lead", "all") and track["lead"]:
            lb = track["lead"][(i // 16) % len(track["lead"])]
            ln = lb[s] if lb else None
            if ln is not None:
                parts.append((t, voice("square", freq(74 + ln), step * 2.2, 0.045, 3600)))
                parts.append((t, voice("triangle", freq(74 + ln), step * 2.6, 0.03, 2000)))
        if layer in ("drums", "all"):
            if track["kick"][s] == "x":
                parts.append((t, kick()))
            if track["snare"][s] == "x":
                parts.append((t, hiss(0.13, 0.17, 1700, "bandpass")))
            if track["hat"][s] == "x":
                parts.append((t, hiss(0.035, 0.05, 7500, "highpass")))
    out = mix(total + 0.5, *parts)
    # o rabo das notas volta para o comeco: loop sem emenda
    tail = out[int(total * SR):]
    out = out[: int(total * SR)]
    out[: len(tail)] += tail
    return out


MENU = {
    "bpm": 96, "chords": [Dm, F, Bb, C], "roots": [38, 41, 34, 36],
    "bass": [0, _, _, _, _, _, 7, _, 0, _, _, _, _, _, _, _],
    "arp": [0, _, 1, _, 2, _, 3, _, 2, _, 1, _, 2, _, _, _],
    "lead": [
        [12, _, _, _, 10, _, 7, _, _, _, _, _, 5, _, _, _],
        [7, _, _, _, 5, _, 3, _, _, _, 5, _, _, _, _, _],
        [5, _, _, _, 3, _, 0, _, _, _, 3, _, 5, _, _, _],
        [7, _, _, _, _, _, _, _, 9, _, _, _, _, _, _, _],
    ],
    "kick": "x.......x.......", "snare": "................", "hat": "..x...x...x...x.",
}

GAME = {
    "bpm": 128, "chords": [Dm, Bb, F, C, Dm, Bb, Gm, A], "roots": [38, 34, 41, 36, 38, 34, 43, 33],
    "bass": [0, _, 0, _, 12, _, 0, _, 0, _, 0, 12, _, 0, 7, _],
    "arp": [0, 1, 2, 3, 2, 1, 0, 1, 2, 3, 2, 1, 2, 1, 0, 1],
    "lead": [
        [12, _, _, 10, _, _, 7, _, _, _, 5, _, 7, _, _, _],
        [5, _, _, 7, _, _, 10, _, _, _, 7, _, 5, _, 3, _],
        [7, _, _, 5, _, _, 3, _, _, _, 0, _, 3, _, 5, _],
        [7, _, 10, _, 12, _, _, _, 10, _, 7, _, 5, _, _, _],
        [12, _, _, 15, _, _, 17, _, 15, _, 12, _, 10, _, _, _],
        [10, _, _, 12, _, _, 10, _, 7, _, _, _, 5, _, _, _],
        [8, _, _, 7, _, _, 5, _, 3, _, 5, _, 7, _, _, _],
        [9, _, _, 9, _, _, 11, _, 12, _, 11, _, 9, _, 7, _],
    ],
    "kick": "x...x...x...x.x.", "snare": "....x.......x...", "hat": "..x...x...x...xx",
}


# ------------------------------------------------------------------ efeitos
LADDER_BASE = 293.66 * 2  # re 5: o jogo sobe a escada pentatonica mudando o pitch


def sfx_pickup():
    b = LADDER_BASE
    return room(mix(0.4,
                    (0, tone("square", b, 0.16, 0.07)),
                    (0, tone("triangle", b * 1.49, 0.26, 0.09)),
                    (0, tone("triangle", b * 2.03, 0.2, 0.05)),
                    (0, noise(0.035, 0.2, "highpass", 5200))), 0.5)


def sfx_fragment():
    notes = [74, 77, 81, 86, 89]
    parts = [(i * 0.055, tone("square", freq(n), 0.22, 0.07)) for i, n in enumerate(notes)]
    parts += [(i * 0.055, tone("triangle", freq(n) * 2, 0.3, 0.05)) for i, n in enumerate(notes)]
    parts += [(0, noise(0.12, 0.12, "bandpass", 1800, 4200, 2)), (0, tone("sine", 520, 0.14, 0.2, 1250))]
    return room(mix(0.8, *parts), 0.6)


def sfx_hurt():
    return room(mix(0.6,
                    (0, tone("sine", 135, 0.13, 0.3, 62)),
                    (0, noise(0.07, 0.12, "lowpass", 700, 150)),
                    (0, noise(0.18, 0.22, "highpass", 7000, 1500)),
                    (0, tone("sawtooth", 600, 0.16, 0.12, 180)),
                    (0, tone("sine", 70, 0.36, 0.5, 30)),
                    (0, noise(0.3, 0.26, "lowpass", 2600, 120))), 0.4)


def sfx_dash():
    return room(mix(0.35,
                    (0, noise(0.16, 0.22, "bandpass", 900, 4200, 1.5)),
                    (0, tone("sine", 320, 0.12, 0.12, 900))), 0.3)


def sfx_bump():
    return mix(0.15, (0, tone("triangle", 220, 0.09, 0.21, 120)), (0, noise(0.05, 0.08, "lowpass", 900, 200)))


def sfx_unlock():
    notes = [62, 69, 74, 77, 81, 86]
    parts = [(i * 0.09, tone("square", freq(n), 0.3, 0.06)) for i, n in enumerate(notes)]
    parts += [(i * 0.09, tone("triangle", freq(n), 0.5, 0.1)) for i, n in enumerate(notes)]
    parts += [(0.5, tone("sine", 70, 0.5, 0.4, 35)), (0.5, noise(0.5, 0.15, "highpass", 6000, 9000))]
    return room(mix(1.6, *parts), 0.7)


def sfx_ignite():
    return room(mix(1.4,
                    (0, noise(1.0, 0.5, "lowpass", 400, 3500)),
                    (0, noise(0.6, 0.25, "bandpass", 600, 2400, 1.2)),
                    (0, tone("sine", 60, 0.8, 0.5, 120)),
                    (0.05, tone("sawtooth", 110, 0.5, 0.06, 220))), 0.5)


def sfx_win():
    seq = [(0, 74), (0.12, 78), (0.24, 81), (0.36, 86), (0.6, 85), (0.72, 86)]
    parts = []
    for t, n in seq:
        d = 0.5 if t < 0.72 else 1.2
        parts += [(t, voice("square", freq(n), d, 0.09, 3600)), (t, voice("triangle", freq(n - 12), d, 0.14, 2000))]
    parts += [(0.72, voice("triangle", freq(62), 1.2, 0.2, 900)), (0.72, voice("triangle", freq(69), 1.2, 0.12, 1800))]
    return room(mix(2.2, *parts), 0.6)


def sfx_ko():
    parts = [(0, noise(0.5, 0.5, "lowpass", 1600, 70)), (0, tone("sine", 85, 0.5, 0.6, 28)),
             (0, tone("square", 190, 0.14, 0.14, 50)), (0, noise(0.25, 0.3, "highpass", 6000, 2000))]
    for i, n in enumerate([69, 65, 62, 57]):
        parts.append((0.45 + i * 0.22, voice("square", freq(n), 0.5 if i < 3 else 1.2, 0.08, 2400)))
        parts.append((0.45 + i * 0.22, voice("triangle", freq(n - 12), 0.5 if i < 3 else 1.2, 0.12, 1200)))
    return room(mix(2.4, *parts), 0.5)


def sfx_ui_move():
    return mix(0.12, (0, tone("triangle", 880, 0.1, 0.08)), (0, noise(0.025, 0.12, "highpass", 6000)))


def sfx_ui_confirm():
    return room(mix(0.3, (0, tone("sine", 660, 0.1, 0.07)), (0.06, tone("sine", 990, 0.14, 0.07)),
                    (0, noise(0.02, 0.1, "highpass", 3500))), 0.3)


def sfx_ui_back():
    return mix(0.15, (0, tone("triangle", 760, 0.08, 0.09, 280)), (0, noise(0.07, 0.14, "bandpass", 3200, 700, 1.6)))


def sfx_ui_deny():
    return mix(0.12, (0, tone("sawtooth", 1500, 0.07, 0.07, 220)))


def sfx_warning():
    """batida de coracao quando o calor esta acabando"""
    return mix(0.4, (0, tone("sine", 70, 0.14, 0.5, 45)), (0.16, tone("sine", 62, 0.16, 0.35, 40)))


def sfx_alert():
    return room(mix(0.4, (0, tone("square", 880, 0.06, 0.06)), (0.07, tone("square", 1320, 0.12, 0.07)),
                    (0, noise(0.1, 0.1, "bandpass", 3800, 1600, 1.2))), 0.3)


def sfx_frost():
    parts = [(i * 0.03, tone("sine", 2400 + i * 400, 0.2, 0.04)) for i in range(5)]
    parts.append((0, noise(0.3, 0.1, "highpass", 8000)))
    return room(mix(0.5, *parts), 0.6)


if __name__ == "__main__":
    write("music_menu.wav", sequence(MENU, 16, "all"), 0.6)
    bars = 16
    base, drums, lead = (sequence(GAME, bars, l) for l in ("base", "drums", "lead"))
    peak = max(np.max(np.abs(base + drums + lead)), 1e-6) / 0.75
    for name, stem in (("music_game_base.wav", base), ("music_game_drums.wav", drums), ("music_game_lead.wav", lead)):
        write(name, stem / peak, normalize=False)

    write("sfx_pickup.wav", trim(sfx_pickup()))
    write("sfx_fragment.wav", trim(sfx_fragment()))
    write("sfx_hurt.wav", trim(sfx_hurt()))
    write("sfx_dash.wav", trim(sfx_dash()), 0.6)
    write("sfx_bump.wav", trim(sfx_bump()), 0.5)
    write("sfx_unlock.wav", trim(sfx_unlock()))
    write("sfx_ignite.wav", trim(sfx_ignite()))
    write("sfx_win.wav", trim(sfx_win()))
    write("sfx_ko.wav", trim(sfx_ko()))
    write("sfx_ui_move.wav", trim(sfx_ui_move()), 0.45)
    write("sfx_ui_confirm.wav", trim(sfx_ui_confirm()), 0.6)
    write("sfx_ui_back.wav", trim(sfx_ui_back()), 0.5)
    write("sfx_ui_deny.wav", trim(sfx_ui_deny()), 0.45)
    write("sfx_warning.wav", trim(sfx_warning()), 0.7)
    write("sfx_alert.wav", trim(sfx_alert()), 0.6)
    write("sfx_frost.wav", trim(sfx_frost()), 0.5)
