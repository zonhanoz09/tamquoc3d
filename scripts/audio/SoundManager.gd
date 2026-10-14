class_name SoundManager
extends Node
## SoundManager - Tự động tổng hợp âm thanh Retro Chiptune / 16-bit
## Tạo âm thanh bằng AudioStreamWAV thủ công không phụ thuộc file ngoài

static var instance: SoundManager = null

var _audio_players: Array[AudioStreamPlayer] = []
const POOL_SIZE = 8
var _pool_idx = 0

func _enter_tree() -> void:
	instance = self

func _ready() -> void:
	# Khởi tạo audio pool
	for i in range(POOL_SIZE):
		var p = AudioStreamPlayer.new()
		add_child(p)
		_audio_players.append(p)

func _get_player() -> AudioStreamPlayer:
	var p = _audio_players[_pool_idx]
	_pool_idx = (_pool_idx + 1) % POOL_SIZE
	return p

## ──────────────────────────────────────────────────────────
## TỔNG HỢP ÂM THANH CHIPTUNE 8-BIT / 16-BIT THỦ CÔNG
## ──────────────────────────────────────────────────────────

func _make_tone_wav(freq_start: float, freq_end: float, duration: float, wave_type: String = "square") -> AudioStreamWAV:
	var sample_rate = 22050
	var num_samples = int(sample_rate * duration)
	var byte_array = PackedByteArray()
	byte_array.resize(num_samples)

	var phase: float = 0.0
	for i in range(num_samples):
		var t = float(i) / float(num_samples)
		var freq = freq_start * (1.0 - t) + freq_end * t
		phase += 2.0 * PI * freq / sample_rate
		if phase > 2.0 * PI:
			phase -= 2.0 * PI

		# Envelope (fade out at end)
		var env = 1.0 - t
		if t < 0.05:
			env = t / 0.05

		var sample_val: float = 0.0
		if wave_type == "square":
			sample_val = 1.0 if sin(phase) > 0.0 else -1.0
		elif wave_type == "triangle":
			sample_val = asin(sin(phase)) * (2.0 / PI)
		elif wave_type == "noise":
			sample_val = randf_range(-1.0, 1.0)
		else: # sine
			sample_val = sin(phase)

		var byte_val = clampi(int(128 + sample_val * env * 110), 0, 255)
		byte_array[i] = byte_val

	var wav = AudioStreamWAV.new()
	wav.format = AudioStreamWAV.FORMAT_8_BITS
	wav.mix_rate = sample_rate
	wav.stereo = false
	wav.data = byte_array
	return wav

## ──────────────────────────────────────────────────────────
## API PHÁT ÂM THANH HIỆU ỨNG CHIẾN ĐẤU
## ──────────────────────────────────────────────────────────

func play_slash() -> void:
	var wav = _make_tone_wav(800.0, 150.0, 0.12, "noise")
	var p = _get_player()
	p.stream = wav
	p.volume_db = -4.0
	p.pitch_scale = randf_range(0.9, 1.2)
	p.play()

func play_hit() -> void:
	var wav = _make_tone_wav(350.0, 80.0, 0.14, "triangle")
	var p = _get_player()
	p.stream = wav
	p.volume_db = -2.0
	p.pitch_scale = randf_range(0.95, 1.1)
	p.play()

func play_crit() -> void:
	var wav = _make_tone_wav(900.0, 60.0, 0.25, "square")
	var p = _get_player()
	p.stream = wav
	p.volume_db = 0.0
	p.pitch_scale = 1.0
	p.play()

func play_block() -> void:
	var wav = _make_tone_wav(1200.0, 600.0, 0.15, "triangle")
	var p = _get_player()
	p.stream = wav
	p.volume_db = -3.0
	p.pitch_scale = 1.1
	p.play()

func play_dodge() -> void:
	var wav = _make_tone_wav(300.0, 850.0, 0.12, "sine")
	var p = _get_player()
	p.stream = wav
	p.volume_db = -5.0
	p.pitch_scale = 1.0
	p.play()

func play_magic() -> void:
	var wav = _make_tone_wav(400.0, 1100.0, 0.28, "square")
	var p = _get_player()
	p.stream = wav
	p.volume_db = -4.0
	p.pitch_scale = randf_range(0.95, 1.15)
	p.play()

func play_heal() -> void:
	var wav = _make_tone_wav(440.0, 880.0, 0.35, "sine")
	var p = _get_player()
	p.stream = wav
	p.volume_db = -3.0
	p.pitch_scale = 1.0
	p.play()

func play_rage() -> void:
	var wav = _make_tone_wav(200.0, 1400.0, 0.45, "square")
	var p = _get_player()
	p.stream = wav
	p.volume_db = 2.0
	p.pitch_scale = 1.0
	p.play()

func play_victory() -> void:
	var wav = _make_tone_wav(523.25, 1046.5, 0.6, "square")
	var p = _get_player()
	p.stream = wav
	p.volume_db = 1.0
	p.pitch_scale = 1.0
	p.play()
