using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Assets.SimpleLocalization;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    private const string SaveGold = "gold";
    private const string SaveHeroHp = "heroHp";
    private const string SaveRound = "roundIndex";
    private const string SaveWin = "won";
    private const string SaveRunStarted = "runStarted";
    private const string SaveEnergy = "energy";
    private const string SaveUpgradeLevelPrefix = "upgradeLevel_";
    private const string SaveLanguage = "Language";
    private const string SaveMusicVolume = "musicVolume";
    private const string SaveSoundVolume = "soundVolume";
    private const string SaveResolutionWidth = "resolutionWidth";
    private const string SaveResolutionHeight = "resolutionHeight";
    private const string SaveResolutionHzNumerator = "resolutionHzNum";
    private const string SaveResolutionHzDenominator = "resolutionHzDen";

    // Уровень релика (сколько раз куплен): relicLevel_RelicTick и т.д.
    private const string SaveRelicLevelPrefix = "relicLevel_";

    [Header("References")]
    public ArrowController arrowController;
    public ZoneManager zoneManager;
    public GameObject enemyPrefab;

    public Transform enemyContainer;

    [Tooltip("Точки врагов из сцены (EnemiesPositions): 0 — центр, дальше бока. Пусто — старые смещения.")]
    public Transform[] enemyPositions;

    [Header("Враги по раундам")]
    [Tooltip("Группа врагов на каждый раунд: индекс = номер раунда − 1. " +
             "Список задаёт врагов по слотам (0 — центр, 1 — левый, 2 — правый) и их количество. " +
             "Пустой список — случайные враги из CharacterLibrary.")]
    public EnemyGroupConfig[] roundEnemyGroups = new EnemyGroupConfig[0];

    /// <summary>Враги одной группы раунда: id берутся из CharacterLibrary.</summary>
    [System.Serializable]
    public class EnemyGroupConfig
    {
        [Tooltip("Враги группы по слотам: 0 — центр, 1 — левый, 2 — правый.")]
        [CharacterId]
        public string[] enemies = new string[0];
    }

    [Tooltip("Библиотека врагов с кадрами Idle/Attack/Defeat. Пусто — грузится из Resources.")]
    public CharacterLibrary characterLibrary;

    [Tooltip("Имя ресурса с библиотекой врагов (Assets/Resources/<имя>.asset).")]
    public string characterLibraryResourceName = "CharacterLibrary";

    [Header("UI")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI lifeText;
    public Image lifeFill;
    public TextMeshProUGUI roundText;
    public TextMeshProUGUI levelInfoText;

    [Header("Settings")]
    public int maxHeroHp = 3;

    [Tooltip("Длительность раунда в секундах по умолчанию (если в кривой сложности 0).")]
    public float roundDuration = 30f;

    [Header("Difficulty (по уровням)")]
    [Tooltip("Кривая сложности: элемент 0 = уровень 1. Если пусто — заполняется значениями по умолчанию.")]
    public RoundBalance[] roundBalance;

    [Header("Upgrades (7 зон, по 10 уровней)")]
    [Tooltip("Порядок: Размер зон, Урон, Таймер, Энергия, Скорость движения зоны, Скорость масштаба зоны, Скорость стрелки.")]
    public UpgradeInfo[] upgrades;

    public float idleArrowSpeed = 90f;

    [Tooltip("Множитель скорости стрелки вне боя (меню): 0.5 — в два раза медленнее. Прокачка на это не влияет.")]
    public float idleArrowSpeedFactor = 0.5f;

    public int damagePerHit = 10;

    [Header("Награды/штрафы зон")]
    [Tooltip("Сколько жизней добавляет зелёная зона (Хил).")]
    public int healAmount = 1;

    [Tooltip("Сколько золота добавляет жёлтая зона.")]
    public int goldZoneAmount = 10;

    [Tooltip("Сколько энергии добавляет синяя зона.")]
    public int energyZoneAmount = 15;

    [Tooltip("Сколько секунд добавляет фиолетовая зона (Таймер).")]
    public float timerZoneSeconds = 3f;

    [Tooltip("Сколько урона герою наносит красная зона-ловушка.")]
    public int trapDamage = 1;

    [Tooltip("Урон герою, если таймер раунда истёк.")]
    public int heroDamageOnTimerFail = 1;

    [Header("Slow-mo")]
    [Tooltip("Time.timeScale, пока зажата правая кнопка мыши.")]
    public float slowMoTimeScale = 0.5f;

    [Tooltip("Изображение слоу-мо (SLow-mo image): его альфа анимируется.")]
    public Image slowMoOverlay;

    [Tooltip("Альфа изображения слоу-мо, пока слоу-мо активно.")]
    [Range(0f, 1f)]
    public float slowMoOverlayAlpha = 0.5f;

    [Tooltip("Длительность появления/исчезновения изображения слоу-мо, сек.")]
    public float slowMoOverlayFadeDuration = 0.2f;

    [Tooltip("Максимальный запас энергии.")]
    public float maxEnergy = 100f;

    [Header("Background")]
    [Tooltip("Чёрная подложка (BackgroundBlack): в бою альфа 0, в главном меню — backgroundBlackMainMenuAlpha.")]
    public SpriteRenderer backgroundBlack;

    [Tooltip("Полноэкранный фон (BackgroundFull): в меню его _GreyscaleBlend = 1, в бою — 0.")]
    public SpriteRenderer backgroundFull;

    [Tooltip("Фоны по порядку: меняются каждые backgroundChangeLevels уровней и идут по кругу.")]
    [SpriteNameList]
    public Sprite[] backgrounds;

    [Tooltip("Через сколько уровней меняется фон.")]
    public int backgroundChangeLevels = 5;

    [Tooltip("Альфа чёрной подложки в главном меню.")]
    [Range(0f, 1f)]
    public float backgroundBlackMainMenuAlpha = 0.5f;

    [Tooltip("Длительность анимации подложки и grayscale фона, сек.")]
    public float backgroundBlackFadeDuration = 0.3f;

    [Tooltip("Расход энергии в секунду в режиме слоу-мо.")]
    public float energyDrainPerSecond = 20f;

    [Tooltip("Восстановление энергии в секунду вне слоу-мо (0 — не восстанавливается).")]
    public float energyRecoverPerSecond;

    [Tooltip("Полоска энергии (Slow-mo (energy)): меняется её ширина.")]
    public RectTransform energyBar;

    [Tooltip("Текст с процентом энергии (Slow-mo txt).")]
    public TextMeshProUGUI energyText;
    public int roundsCount = 30;
    public float enemyMoveDuration = 0.2f;
    public float sideOffsetWorld = 4.45f;
    public Transform ringTransform;
    public float centerEnemyScale = 1f;
    public float sideEnemyScale = 0.75f;
    public float battleStartDelay = 2f;

    [Header("Start Zones")]
    public Transform playZone;
    public Transform continueZone;
    public Transform fightZone;
    public Transform exitZone;
    public Transform settingsZone;
    public Transform updatesZone;

    [Tooltip("Зона SHOP: видна только между боями (после победы над группой врагов).")]
    public Transform shopZone;

    [Tooltip("Окно между боями (Window_after_fight): FIGHT / SHOP / UPDATES. Включается после победы.")]
    public Transform afterFightWindow;

    [Tooltip("Окно магазина (Window_shop): открывается по зоне SHOP.")]
    public Transform shopWindow;
    [Tooltip("Зона BACK внутри окна магазина (возврат к Window_after_fight).")]
    public Transform shopBackZone;

    [Header("Pause Window")]
    [Tooltip("Окно паузы (Window_pause).")]
    public Transform pauseWindow;

    [Tooltip("Зона RESUME в окне паузы.")]
    public Transform pauseResumeZone;

    [Tooltip("Зона MENU в окне паузы.")]
    public Transform pauseMenuZone;

    [Tooltip("Зона SETTINGS в окне паузы.")]
    public Transform pauseSettingsZone;

    [Tooltip("Надпись PAUSE (объект Pause): показывается вместе с окном паузы.")]
    public GameObject pauseLabel;

    [Header("Fly Objects (покупки в магазине)")]
    [Tooltip("Префаб Fly_health — летит от зоны HP к Life.")]
    public GameObject flyHealthPrefab;

    [Tooltip("Префаб Fly_energy — летит к Slow-mo.")]
    public GameObject flyEnergyPrefab;

    [Tooltip("Префаб Fly_gold — летит к Gold.")]
    public GameObject flyGoldPrefab;

    [Tooltip("Префаб Fly_timer — летит к Timer.")]
    public GameObject flyTimerPrefab;

    [Tooltip("Длительность полёта Fly-объекта, сек.")]
    public float flyObjectDuration = 0.5f;

    [Header("Damage Flash")]
    [Tooltip("Изображение GetDamage image: вспыхивает при получении урона.")]
    public Image damageOverlay;

    [Tooltip("Время вспышки урона (альфа 0 → 1), сек.")]
    public float damageFlashInDuration = 0.08f;

    [Tooltip("Время затухания вспышки (альфа 1 → 0), сек.")]
    public float damageFlashOutDuration = 0.35f;

    [Tooltip("Окно окончания забега (Window_end): показывается после победы/проигрыша.")]
    public Transform endWindow;

    [Tooltip("Зона ZoneShop_HP — полное восстановление здоровья за shopHpPrice.")]
    public Transform shopHpZone;

    [Tooltip("Зона ZoneShop_MP — полное восстановление энергии за shopEnergyPrice.")]
    public Transform shopEnergyZone;

    [Tooltip("Ценник зоны ZoneShop_HP (Cell_).")]
    public TMP_Text shopHpPriceText;

    [Tooltip("Ценник зоны ZoneShop_MP (Cell_).")]
    public TMP_Text shopEnergyPriceText;

    [Tooltip("Цена полного восстановления здоровья в магазине.")]
    public int shopHpPrice = 20;

    [Tooltip("Цена полного восстановления энергии в магазине.")]
    public int shopEnergyPrice = 20;

    [Header("Shop Items (релики)")]
    [Tooltip("Релики магазина: ключи названия/описания в ClickLocalization.txt и числа из описаний (по умолчанию 19 реликов).")]
    public RelicInfo[] relics;

    [Tooltip("Зоны пассивок в Window_shop (ZoneShop_item_1..3).")]
    public Transform[] shopItemZones;

    [Tooltip("Ценники пассивок (Cell_ внутри зон).")]
    public TMP_Text[] shopItemPriceTexts;

    [Tooltip("Базовая цена любого релика в магазине.")]
    public int shopItemPrice = 50;

    [Tooltip("Во сколько раз дорожают оставшиеся релики после каждой покупки.")]
    public float shopItemPriceGrowth = 1.5f;

    [Tooltip("Названия реликов (Name_Text (TMP) внутри зон). Пусто — ищется по имени дочернего объекта.")]
    public TMP_Text[] shopItemNameTexts;

    [Tooltip("Описания реликов (Desc._Text (TMP) внутри зон). Пусто — ищется по имени дочернего объекта.")]
    public TMP_Text[] shopItemDescTexts;

    [Tooltip("Объекты Tittle зон реликов (в них Name_Text и Desc._Text): выключаются после покупки.")]
    public Transform[] shopItemTitles;

    [Tooltip("Цвет надписи SOLD на проданной зоне.")]
    public Color shopSoldColor = new Color32(0x8C, 0x8C, 0x8C, 0xFF);

    [Tooltip("Время полёта Tittle к Info_relics при покупке (со скейлом до 0), сек.")]
    public float relicTitleFlyDuration = 0.45f;

    public Transform mainMenuZone;

    [Header("Upgrade Window")]
    [Tooltip("Контейнер окон с зонами прокачки (Window_update).")]
    public Transform upgradeRoot;

    [Tooltip("Зона BACK внутри окна прокачки.")]
    public Transform backZone;

    [Header("Settings Windows")]
    [Tooltip("Окно настроек (Window_settings).")]
    public Transform settingsRoot;

    [Tooltip("Зона MUSIC в окне настроек.")]
    public Transform settingsMusicZone;

    [Tooltip("Зона SOUND в окне настроек.")]
    public Transform settingsSoundZone;

    [Tooltip("Зона LANGUAGE в окне настроек.")]
    public Transform settingsLanguageZone;

    [Tooltip("Зона RESOLUTION в окне настроек.")]
    public Transform settingsResolutionZone;

    [Tooltip("Зона BACK в окне настроек.")]
    public Transform settingsBackZone;

    [Tooltip("Окно настроек музыки (Window_settings_music).")]
    public Transform musicRoot;

    [Tooltip("Зона BACK в окне музыки.")]
    public Transform musicBackZone;

    [Tooltip("Окно настроек звука (Window_settings_sound).")]
    public Transform soundRoot;

    [Tooltip("Зона BACK в окне звука.")]
    public Transform soundBackZone;

    [Tooltip("Окно выбора языка (Window_settings_language).")]
    public Transform languageRoot;

    [Tooltip("Зона BACK в окне выбора языка.")]
    public Transform languageBackZone;

    [Tooltip("Окно разрешения (Window_settings_resolution).")]
    public Transform resolutionRoot;

    [Tooltip("Зона BACK в окне разрешения.")]
    public Transform resolutionBackZone;

    [Tooltip("Зоны языков в окне выбора языка (порядок ZoneLanguage_1..11).")]
    public Transform[] languageZones;

    [Tooltip("Зоны, у которых при наведении не меняется fill (ZoneMusicBack, ZoneSoundBack).")]
    public Transform[] noHoverFillZones;

    [Header("Audio")]
    [Range(0f, 1f)]
    [Tooltip("Текущая громкость музыки (1 = 100%).")]
    public float musicVolume = 1f;

    [Range(0f, 1f)]
    [Tooltip("Текущая громкость звуков (1 = 100%).")]
    public float soundVolume = 1f;

    [Tooltip("Слайдер громкости музыки (ZoneMusic в окне музыки).")]
    public Transform musicVolumeZone;

    [Tooltip("Парная зона музыки, где тоже пишется процент (ZoneMusicBack).")]
    public Transform musicVolumeBackZone;

    [Tooltip("Слайдер громкости звуков (ZoneSound в окне звука).")]
    public Transform soundVolumeZone;

    [Tooltip("Парная зона звука, где тоже пишется процент (ZoneSoundBack).")]
    public Transform soundVolumeBackZone;

    [Tooltip("Громкость растёт от центра сектора к краям (шкала). Иначе — от края к краю.")]
    public bool volumeFromCenter = true;

    [Tooltip("Инвертировать направление роста сектора громкости (если заполнение идёт в другую сторону).")]
    public bool invertVolumeDirection;

    [Tooltip("Замедление стрелки после покупки прокачки: доля от текущей скорости (0.1 = 10%).")]
    [Range(0.01f, 1f)]
    public float upgradeArrowSlowFactor = 0.1f;

    [Tooltip("Длительность замедления стрелки после покупки прокачки, сек.")]
    public float upgradeArrowSlowDuration = 1f;

    public float startZoneWidth = 54f;

    [Tooltip("Добавка к углу зоны, если сектор в префабе смотрит не вверх (обычно 0).")]
    public float startZoneAngleOffset = 0f;

    public float startZoneFadeDuration = 0.12f;

    [Tooltip("На сколько процентов расширяется сектор зоны, когда в ней стоит стрелка.")]
    public float startZoneHoverPercent = 3.3f;

    [Tooltip("Масштаб надписи зоны, когда в ней стоит стрелка.")]
    public float startZoneHoverLabelScale = 1.1f;

    [Header("Finish UI")]
    public GameObject levelWinObject;
    public GameObject levelLoseObject;

    [Header("Battle UI Roots")]
    public GameObject timerRoot;
    public GameObject healthRoot;
    public GameObject lifeRoot;

    [Tooltip("Slow-mo: корень UI энергии — виден в игре, скрыт в главном меню.")]
    public GameObject slowMoRoot;

    [Tooltip("GameName — логотип текста: виден только в главном меню.")]
    public GameObject gameName;

    [Tooltip("Объект Gold (золото героя): включается при старте забега из меню.")]
    public GameObject goldRoot;

    [Tooltip("Объект Info_relics: включается на старте уровня, если куплено больше одного релика, и сразу после покупки.")]
    public GameObject infoRelics;

    [Tooltip("Текст внутри Info_relics (Info_relics_Text (TMP)): список купленных реликов при наведении.")]
    public TMP_Text infoRelicsText;

    [Tooltip("Задник подсказки (Info_relics_back): включается только при наведении.")]
    public GameObject infoRelicsBack;

    [Tooltip("Размер шрифта названия релика в Info_relics.")]
    public float infoRelicsNameFontSize = 30f;

    [Tooltip("Размер шрифта описания релика в Info_relics.")]
    public float infoRelicsDescFontSize = 24f;

    [Tooltip("Во сколько раз можно превысить радиус кольца кликом, чтобы он считался кликом по зоне (надписи выходят за кольцо).")]
    public float pointerRadiusFactor = 1.5f;

    private enum State
    {
        Idle,
        Fighting,
        Finished
    }

    public enum StartZoneAction
    {
        None,
        Fight,
        Exit,
        MainMenu,
        Updates,
        Back,
        Upgrade,
        Settings,
        SettingsMusic,
        SettingsSound,
        SettingsLanguage,
        SettingsResolution,
        SettingsBack,
        SettingsWindowBack,
        Language,
        VolumeMusic,
        VolumeSound,
        Resolution,
        NewGame,
        Continue,
        Shop,
        ShopBack,
        ShopRestoreHp,
        ShopRestoreEnergy,
        ShopBuyItem,
        PauseResume,
        PauseMenu,
        PauseSettings
    }

    /// <summary>
    /// Релик магазина: ключи локализации (12 языков) и числа из описания (для баланса/эффектов).
    /// </summary>
    [System.Serializable]
    public class RelicInfo
    {
        [Tooltip("Ключ названия в ClickLocalization.txt, например Localization.RelicTick.")]
        public string nameKey;

        [Tooltip("Ключ описания в ClickLocalization.txt. Пусто — берётся nameKey + \"Desc\".")]
        public string descKey;

        [Tooltip("Число релика из описания: секунды / энергия / HP / золото / проценты (для эффектов; сам текст берётся из локализации).")]
        public float value;

        [Tooltip("Шанс срабатывания, %. 0 — релик без шанса.")]
        public float chance;

        // Сколько раз релик куплен (уровень). Живёт в FBPP, в сцене не сохраняется.
        [System.NonSerialized]
        public int level;
    }

    // Разрешения, назначенные показанным зонам окна разрешения (индекс = zoneIndex зоны).
    private readonly List<Resolution> shownResolutions = new List<Resolution>();

    private int appliedResolutionIndex = -1;

    // Имена языков в порядке зон окна выбора языка (ZoneLanguage_1..11).
    private static readonly string[] LanguageNames =
    {
        "English", "Russian", "Spanish", "French", "German", "Turkish",
        "Polish", "Chinese", "Japanese", "Korean", "Portuguese"
    };

    // Названия языков так, как они пишутся на самом языке (не переводятся).
    private static readonly string[] LanguageNativeNames =
    {
        "ENGLISH", "РУССКИЙ", "ESPAÑOL", "FRANÇAIS", "DEUTSCH", "TÜRKÇE",
        "POLSKI", "中文", "日本語", "한국어", "PORTUGUÊS"
    };

    private readonly List<EnemyView> enemies = new List<EnemyView>();
    private readonly List<string> logLines = new List<string>();
    private readonly List<(float angle, float width, StartZoneAction action, ZoneFade fade, int zoneIndex)> activeStartZones = new List<(float, float, StartZoneAction, ZoneFade, int)>();

    private static readonly Color LogRoundColor = new Color(0.2f, 1f, 0.2f);
    private static readonly Color LogDamageColor = Color.white;
    private static readonly Color LogGoldColor = new Color(1f, 0.85f, 0.1f);
    private static readonly Color LogHeroDamageColor = new Color(1f, 0.25f, 0.25f);
    private static readonly Color LogMissColor = new Color(1f, 0.55f, 0f);
    private static readonly Color LogHealColor = new Color(0.3f, 1f, 0.3f);
    private static readonly Color LogEnergyColor = new Color(0.3f, 0.6f, 1f);
    private static readonly Color LogTimerColor = new Color(0.7f, 0.35f, 1f);
    private static readonly Color LogRelicColor = new Color(0.85f, 0.45f, 1f);

    private State state = State.Idle;
    private int heroHp;
    private int gold;
    private int roundIndex;

    // Забег начат и ещё не закончен (проигрыш/победа) — для зоны CONTINUE.
    private bool runStarted;
    private float timeLeft;
    private float startDelayLeft;
    private bool roundStarting;
    private float energy;
    private float energyBaseWidth;
    private float lastEnergyWidth = -1f;
    private int lastEnergyPercent = -1;
    private int lastTimerTenths = -1;
    private bool slowMoActive;
    private float slowMoOverlayValue;
    private bool slowMoOverlayCached;

    // Покупка в магазине / комбо-зоны: значения выдаются сразу, UI обновляется после прилёта Fly-объекта.
    private bool suppressLifeUi;
    private bool suppressEnergyUi;
    private bool suppressGoldUi;
    private bool suppressTimerUi;

    // Вспышка GetDamage image при получении урона.
    private float damageOverlayValue;
    private bool damageOverlayCached;
    private bool damageFlashActive;
    private bool waitingDamageFlash;

    // Пауза во время боя (Esc): мир заморожен, активно только окно паузы.
    private bool paused;
    private bool settingsFromPause;

    // Пробная зона стрелки для проверки попадания (без new ZoneData на каждый клик).
    private ZoneData arrowProbe;
    private bool shopPriceColorsCached;
    private Color shopHpBasePriceColor = Color.white;
    private Color shopEnergyBasePriceColor = Color.white;

    // Зоны/ценники пассивок магазина (сами пассивки ещё не реализованы).
    private static readonly int ShopItemCount = 3;
    private Color[] shopItemBasePriceColors;

    // Ценник купленного релика.
    private const string ShopSoldKey = "Localization.Sold";

    // Суффикс ключа описания релика: Localization.RelicTick -> Localization.RelicTickDesc.
    private const string ShopRelicDescSuffix = "Desc";

    // Числа в описании релика (0,1 / +5 / 200%). Разделитель дроби сохраняем как в оригинале.
    // Без RegexOptions.Compiled — безопаснее для IL2CPP, вызывается редко.
    private static readonly Regex RelicNumberRegex = new Regex(@"\d+(?:[.,]\d+)?");

    // Ключи реликов — по ним ищется уровень/значения в эффектах.
    private const string RelicTickKey = "Localization.RelicTick";
    private const string RelicSparkKey = "Localization.RelicSpark";
    private const string RelicPulseKey = "Localization.RelicPulse";
    private const string RelicHourglassKey = "Localization.RelicHourglass";
    private const string RelicThriftKey = "Localization.RelicThrift";
    private const string RelicTimeTrophyKey = "Localization.RelicTimeTrophy";
    private const string RelicGeneratorKey = "Localization.RelicGenerator";
    private const string RelicAdrenalineKey = "Localization.RelicAdrenaline";
    private const string RelicLooterKey = "Localization.RelicLooter";
    private const string RelicDeadeyeKey = "Localization.RelicDeadeye";
    private const string RelicTimeShieldKey = "Localization.RelicTimeShield";
    private const string RelicChainReactionKey = "Localization.RelicChainReaction";
    private const string RelicMagnetKey = "Localization.RelicMagnet";
    private const string RelicMomentumKey = "Localization.RelicMomentum";
    private const string RelicReservoirKey = "Localization.RelicReservoir";
    private const string RelicStreakKey = "Localization.RelicStreak";
    private const string RelicBleedKey = "Localization.RelicBleed";
    private const string RelicTailwindKey = "Localization.RelicTailwind";
    private const string RelicAnchorKey = "Localization.RelicAnchor";

    // Сколько подряд зон было поражено без промаха (ЦЕПОЧКА).
    private int relicStreakHits;

    // Пауза боя на анимации врага: смерть (defeat) или атака по таймеру.
    private bool enemyDeathPause;
    private int pendingDefeats;

    // Идёт анимация закрытия окна паузы — повторные клики не обрабатываем.
    private bool pauseTransitionInProgress;

    // Враги текущего раунда: при перезапуске после урона остаются теми же.
    private List<CharacterData> roundCharacters;

    // Трупы последних врагов группы: лежат на сцене, пока не начнётся новый бой.
    private readonly List<EnemyView> lingeringEnemies = new List<EnemyView>();

    // Герой погиб от атаки врага: окно проигрыша показываем после анимации.
    private bool pendingHeroDeath;

    // Таймер уже просрочен и сейчас идёт атака врага — повторно не срабатываем.
    private bool timerFailInProgress;

    // Накопитель дробного урона от кровотечения.
    private float relicBleedAccumulator;

    // Базовый максимум энергии из инспектора (без бонуса ИСКРЫ) — для пересчёта и процента в UI.
    private float baseMaxEnergy;

    // Базовый максимум HP из инспектора (без бонуса ПУЛЬСА).
    private int baseMaxHeroHp;

    // Подсказка Info_relics: перестраиваем только когда что-то изменилось.
    private bool infoRelicsHovered;
    private bool infoRelicsTextDirty = true;

    // Все релики игры (названия): 19 штук, описания лежат под ключом с суффиксом Desc.
    // Значения по умолчанию — списком в инспекторе GameManager (поле relics).

    // Витрина: продан ли релик в зоне и какой релик туда выпал (индекс в relics).
    private bool[] shopItemSold;
    private int[] shopItemRelicIndex;

    // Текущие цены витрины: растут после каждой покупки и сбрасываются при новой витрине.
    private int[] shopItemCurrentPrices;

    // Исходные позиция/скейл Tittle — нужно вернуть их при новой витрине.
    private Vector3[] shopItemTitleHomePos;
    private Vector3[] shopItemTitleHomeScale;

    // ---------------------------------------------------------------- lifecycle

    private void Awake()
    {
        ApplyDefaults();
        EnsureEnemyContainer();

        // Библиотека врагов грузится из Resources: иначе пришлось бы разводить сотни спрайтов в сцене.
        if (characterLibrary == null && !string.IsNullOrEmpty(characterLibraryResourceName))
            characterLibrary = Resources.Load<CharacterLibrary>(characterLibraryResourceName);

        if (energyBar != null)
            energyBaseWidth = Mathf.Abs(energyBar.sizeDelta.x);

        // Плавное появление/исчезновение стартовых зон через _FillAmount.
        PrepareZoneFade(playZone);
        PrepareZoneFade(fightZone);
        PrepareZoneFade(exitZone);
        PrepareZoneFade(settingsZone);
        PrepareZoneFade(updatesZone);
        PrepareZoneFade(shopZone);
        PrepareZoneFade(mainMenuZone);
        PrepareZoneFade(backZone);

        SetZoneActive(playZone, false, true);
        SetZoneActive(fightZone, false, true);
        SetZoneActive(exitZone, false, true);
        SetZoneActive(settingsZone, false, true);
        SetZoneActive(updatesZone, false, true);
        SetZoneActive(shopZone, false, true);
        SetZoneActive(mainMenuZone, false, true);
        SetWindowActive(afterFightWindow, false);
        HideShopWindow();
        PrepareShopPrices();
        HideUpgradeWindow();

        // Окна настроек: готовим зоны (ZoneFade) и прячем до выбора SETTINGS.
        PrepareSettingsWindows();
        HideSettingsWindows();

        // Надпись раунда видна только в бою.
        SetRoundLabelVisible(false);
        SetActive(levelWinObject, false);
        SetActive(levelLoseObject, false);

        // Life и Slow-mo — боевой HUD: в главном меню их не показываем, а GameName наоборот — только в меню.
        SetInGameHud(false);

        // Золото показывается только после старта забега.
        SetActive(goldRoot, false);

        // Info_relics — только когда куплено больше одного релика (и сразу после покупки).
        SetInfoRelicsVisible(false);

        // Текст подсказки пишет GameManager — автоперевод ему не нужен.
        DisableTranslate(infoRelicsText != null ? infoRelicsText.transform : null);

        // Задник подсказки появляется только при наведении.
        SetActive(infoRelicsBack, false);

        // Окно паузы всегда скрыто до Esc в бою.
        HidePauseWindow();

        // Окно окончания забега показывается только после победы/проигрыша.
        SetWindowActive(endWindow, false);
    }

    private void Start()
    {
        LoadProgress();
        ApplySavedResolution();

        // Текущий уровень мог быть сохранён — сразу ставим его фон в меню.
        ApplyRoundBackground();

        // На старте игры показывается главное меню — подложка и grayscale сразу в меню-виде.
        SetBackgroundMenu(true, true);

        PrepareRound(true);
        UpdateEnergyUI();
    }

    private void Update()
    {
        // Подсказка со списком реликов при наведении на Info_relics.
        UpdateInfoRelicsHover();

        // Оверлей слоу-мо плавно появляется к 0.5 и гаснет к 0 (независимо от timeScale).
        UpdateSlowMoOverlay();

        // Вспышка урона: 0 → 1 → 0.
        UpdateDamageOverlay();

        // Урон от таймера: ждём, пока погаснет GetDamage image, — бой в это время стоит.
        if (waitingDamageFlash)
            return;

        // Пауза боя на анимацию врага (defeat): таймер, стрелка и зоны стоят.
        if (enemyDeathPause)
            return;

        // Пауза: мир заморожен (Time.timeScale = 0), работают только зоны окна паузы.
        if (paused)
        {
            UpdateStartZoneHighlight();

            // Esc: из настроек возвращает в паузу, из паузы — возобновляет игру.
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (settingsFromPause)
                {
                    settingsFromPause = false;
                    ShowPauseWindow();
                }
                else
                {
                    ResumeGame();
                }

                return;
            }

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
                OnPauseTap();

            return;
        }

        UpdateSlowMo();
        UpdateStartZoneHighlight();

        if (state == State.Idle)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
                OnIdleTap();

            return;
        }

        if (state != State.Fighting)
        {
            // После победы/проигрыша стрелка крутится и ждёт клика по зоне MENU.
            if (state == State.Finished && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))
                OnFinishedTap();

            return;
        }

        // Escape во время боя — открыть окно паузы.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OpenPauseWindow();
            return;
        }

        // Задержка старта боя: зоны ещё не созданы, таймер стоит на полном значении.
        if (roundStarting)
        {
            startDelayLeft -= Time.deltaTime;

            if (startDelayLeft > 0f)
            {
                UpdateTimerUI();
                return;
            }

            roundStarting = false;
            startDelayLeft = 0f;
            BeginRoundZones();
        }

        timeLeft -= Time.deltaTime;

        if (timeLeft <= 0f)
        {
            timeLeft = 0f;
            UpdateTimerUI();
            OnTimerFailed();
            return;
        }

        UpdateTimerUI();

        // РЕЗЕРВУАР и КРОВОТЕЧЕНИЕ работают каждый кадр боя.
        UpdateRelicBattleEffects(Time.deltaTime);

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            OnPlayerTap();
    }

    // ---------------------------------------------------------------- setup

    private void ApplyDefaults()
    {
        if (maxHeroHp <= 0) maxHeroHp = 3;

        // Базовый максимум HP: ПУЛЬС добавляет к нему value × уровень.
        baseMaxHeroHp = maxHeroHp;
        if (roundDuration <= 0f) roundDuration = 30f;
        if (idleArrowSpeed <= 0f) idleArrowSpeed = 90f;
        if (idleArrowSpeedFactor <= 0f) idleArrowSpeedFactor = 0.5f;
        if (damagePerHit <= 0) damagePerHit = 10;
        if (heroDamageOnTimerFail <= 0) heroDamageOnTimerFail = 1;
        if (roundsCount <= 0) roundsCount = 30;
        roundsCount = Mathf.Clamp(roundsCount, 1, 30);

        EnsureBalanceCurve();
        EnsureUpgrades();
        EnsureShopItems();

        if (enemyMoveDuration <= 0f) enemyMoveDuration = 0.2f;
        if (sideOffsetWorld <= 0f) sideOffsetWorld = 4.45f;
        if (centerEnemyScale <= 0f) centerEnemyScale = 1f;
        if (sideEnemyScale <= 0f) sideEnemyScale = 0.75f;
        if (battleStartDelay < 0f) battleStartDelay = 0f;

        if (maxEnergy <= 0f) maxEnergy = 100f;

        // Запоминаем базовый максимум: ИСКРА добавляет к нему value × уровень.
        baseMaxEnergy = maxEnergy;

        if (energyDrainPerSecond < 0f) energyDrainPerSecond = 0f;
        if (energyRecoverPerSecond < 0f) energyRecoverPerSecond = 0f;
        slowMoTimeScale = Mathf.Clamp(slowMoTimeScale <= 0f ? 0.5f : slowMoTimeScale, 0.05f, 1f);

        upgradeArrowSlowFactor = Mathf.Clamp(upgradeArrowSlowFactor <= 0f ? 0.1f : upgradeArrowSlowFactor, 0.01f, 1f);
        if (upgradeArrowSlowDuration < 0f) upgradeArrowSlowDuration = 0f;

        musicVolume = Mathf.Clamp01(musicVolume);
        soundVolume = Mathf.Clamp01(soundVolume);

        if (pointerRadiusFactor < 1f) pointerRadiusFactor = 1f;
    }

    private void EnsureEnemyContainer()
    {
        if (enemyContainer != null)
            return;
        GameObject go = new GameObject("EnemyContainer");

        if (ringTransform != null)
            go.transform.position = ringTransform.position;

        enemyContainer = go.transform;
    }

    // ---------------------------------------------------------------- flow

    /// <summary>
    /// Скорость стрелки вне боя. Не зависит от прокачки «Скорость стрелки».
    /// </summary>
    private float IdleArrowSpeed => Mathf.Max(1f, idleArrowSpeed * idleArrowSpeedFactor);

    private void OnIdleTap()
    {
        if (activeStartZones.Count == 0)
        {
            Debug.LogWarning("[GameManager] Нет активных стартовых зон — проверьте поля Start Zones.");
            return;
        }

        // 1) Клик реально попал в сектор зоны — срабатываем сразу, не ждём стрелку.
        if (TryGetPointerAngle(out float pointerAngle) && TryActivateStartZone(pointerAngle))
            return;

        // 2) Фолбэк: стрелка смотрит в зону.
        float arrowAngle = arrowController != null ? arrowController.ArrowAngle : 0f;
        if (TryActivateStartZone(arrowAngle))
            return;

        Debug.Log(string.Format("[Idle] Клик вне стартовых зон: стрелка {0:F1}°, активно зон {1}", arrowAngle, activeStartZones.Count));
    }

    /// <summary>
    /// Может ли зона реагировать на наведение и клик.
    /// </summary>
    private bool IsZoneInteractive((float angle, float width, StartZoneAction action, ZoneFade fade, int zoneIndex) zone)
    {
        switch (zone.action)
        {
            case StartZoneAction.Upgrade:
                return IsUpgradeAvailable(zone.zoneIndex);
            case StartZoneAction.Resolution:
                return !IsResolutionZoneCurrent(zone.zoneIndex);
            case StartZoneAction.ShopRestoreHp:
                return IsShopHpAvailable;
            case StartZoneAction.ShopRestoreEnergy:
                return IsShopEnergyAvailable;
            case StartZoneAction.ShopBuyItem:
                return IsShopItemAvailable(zone.zoneIndex);
            default:
                return true;
        }
    }

    /// <summary>
    /// Зона соответствует уже установленному разрешению.
    /// </summary>
    private bool IsResolutionZoneCurrent(int resolutionIndex)
    {
        if (resolutionIndex < 0 || resolutionIndex >= shownResolutions.Count)
            return false;

        // Если игрок уже выбрал вариант в этом окне — ориентируемся только на него,
        // иначе (в редакторе Screen не обновляется сразу) старый вариант тоже считался бы текущим.
        if (appliedResolutionIndex >= 0)
            return resolutionIndex == appliedResolutionIndex;

        return IsCurrentResolution(shownResolutions[resolutionIndex]);
    }

    /// <summary>
    /// Активирует стартовую зону, в которую попал угол. Возвращает true, если клик обработан.
    /// Побеждает самая узкая подходящая зона, чтобы широкие сектора не перекрывали кнопки.
    /// </summary>
    private bool TryActivateStartZone(float angle)
    {
        float arrowHalfWidth = arrowController != null ? arrowController.ArrowWidth / 2f : 0f;

        // 1) Доступная зона с действием. Декоративные (None) зоны клик не перехватывают:
        //    иначе наложенная зона-заглушка гасит клик по кнопке под ней.
        if (TryFindStartZone(angle, arrowHalfWidth, true, true, out var entry))
            return ActivateStartZone(entry, angle);

        // 2) Зона с действием есть, но недоступна (мало золота, текущее разрешение):
        //    клик поглощаем и пишем причину.
        if (TryFindStartZone(angle, arrowHalfWidth, false, true, out var blocked))
        {
            LogBlockedZone(blocked);
            return true;
        }

        // 3) Доступная декоративная зона — клик поглощаем без действий.
        if (TryFindStartZone(angle, arrowHalfWidth, true, false, out _))
            return true;

        return false;
    }

    /// <summary>
    /// Ищет самую узкую зону под углом. interactiveOnly — пропускать недоступные зоны,
    /// requireAction — пропускать декоративные зоны без действия.
    /// </summary>
    private bool TryFindStartZone(
        float angle,
        float arrowHalfWidth,
        bool interactiveOnly,
        bool requireAction,
        out (float angle, float width, StartZoneAction action, ZoneFade fade, int zoneIndex) result)
    {
        result = default;

        int bestIndex = -1;
        float bestWidth = float.MaxValue;

        for (int i = 0; i < activeStartZones.Count; i++)
        {
            var candidate = activeStartZones[i];
            float diff = Mathf.Abs(Mathf.DeltaAngle(angle, candidate.angle));

            if (diff > candidate.width / 2f + arrowHalfWidth)
                continue;

            if (requireAction && candidate.action == StartZoneAction.None)
                continue;

            // Недоступные зоны (прокачка не по карману, нет золота, текущее разрешение) пропускаем.
            if (interactiveOnly && !IsZoneInteractive(candidate))
                continue;

            bool better;

            if (bestIndex < 0)
                better = true;
            else if (candidate.width < bestWidth - 0.01f)
                better = true;
            else if (Mathf.Abs(candidate.width - bestWidth) <= 0.01f
                     && candidate.action != StartZoneAction.None
                     && activeStartZones[bestIndex].action == StartZoneAction.None)
                better = true; // при равной ширине выбираем зону с действием, а не декоративную
            else
                better = false;

            if (better)
            {
                bestWidth = candidate.width;
                bestIndex = i;
            }
        }

        if (bestIndex < 0)
            return false;

        result = activeStartZones[bestIndex];
        return true;
    }

    private void LogBlockedZone((float angle, float width, StartZoneAction action, ZoneFade fade, int zoneIndex) zone)
    {
        switch (zone.action)
        {
            case StartZoneAction.ShopRestoreHp:
                Debug.Log(string.Format("[Shop] Не хватает золота: HP стоит {0}, есть {1}", shopHpPrice, gold));
                break;
            case StartZoneAction.ShopRestoreEnergy:
                Debug.Log(string.Format("[Shop] Не хватает золота: энергия стоит {0}, есть {1}", shopEnergyPrice, gold));
                break;
            case StartZoneAction.ShopBuyItem:
                if (IsShopItemSold(zone.zoneIndex))
                    Debug.Log("[Shop] Этот релик уже куплен.");
                else
                    Debug.Log(string.Format("[Shop] Не хватает золота: релик стоит {0}, есть {1}", GetShopItemPrice(zone.zoneIndex), gold));
                break;
            case StartZoneAction.Upgrade:
                Debug.Log(string.Format("[Upgrade] Зона недоступна (цена {0}, золото {1})",
                    GetUpgrade(zone.zoneIndex) != null ? GetUpgrade(zone.zoneIndex).NextPrice : 0, gold));
                break;
            case StartZoneAction.Resolution:
                Debug.Log("[Settings] Это разрешение уже выбрано.");
                break;
            default:
                Debug.Log("[GameManager] Зона сейчас недоступна.");
                break;
        }
    }

    /// <summary>
    /// Выполняет действие зоны. Клик по найденной зоне считается обработанным всегда.
    /// </summary>
    private bool ActivateStartZone((float angle, float width, StartZoneAction action, ZoneFade fade, int zoneIndex) entry, float angle)
    {
        switch (entry.action)
        {
            case StartZoneAction.Fight:
                StartBattle();
                break;
            case StartZoneAction.NewGame:
                StartNewRun();
                break;
            case StartZoneAction.Continue:
                StartBattle();
                break;
            case StartZoneAction.Exit:
                QuitGame();
                break;
            case StartZoneAction.MainMenu:
                ResetToMainMenu();
                break;
            case StartZoneAction.Updates:
                ShowUpgradeWindow();
                break;
            case StartZoneAction.Shop:
                ShowShopWindow();
                break;
            case StartZoneAction.ShopBack:
                // Выход из магазина — обратно к Window_after_fight (FIGHT / SHOP / UPDATES).
                ShowFightZoneOnly();
                break;
            case StartZoneAction.ShopRestoreHp:
                // Полное HP или не хватает золота — золото не списывается, причина пишется в лог.
                TryShopRestoreHp();
                break;
            case StartZoneAction.ShopRestoreEnergy:
                TryShopRestoreEnergy();
                break;
            case StartZoneAction.ShopBuyItem:
                TryShopBuyItem(entry.zoneIndex);
                break;
            case StartZoneAction.PauseResume:
                ResumeGame();
                break;
            case StartZoneAction.PauseMenu:
                // Меню из паузы НЕ сбрасывает прогресс забега (для этого есть CONTINUE).
                ExitRunToMenu();
                break;
            case StartZoneAction.PauseSettings:
                settingsFromPause = true;

                // Окно паузы закрывается только после того, как погасли его зоны.
                HideZonesAndWindow(pauseWindow, () =>
                {
                    SetActive(pauseLabel, false);
                    ShowSettingsZones();
                });
                break;
            case StartZoneAction.Back:
                // Выход из окна прокачки — обратно к зонам FIGHT и UPDATES.
                ShowFightZoneOnly();
                break;
            case StartZoneAction.Upgrade:
                // Недоступная прокачка (максимум или не хватает золота) не покупается.
                TryBuyUpgrade(entry.zoneIndex);
                break;
            case StartZoneAction.Settings:
                settingsFromPause = false;
                ShowSettingsZones();
                break;
            case StartZoneAction.SettingsMusic:
                ShowSettingsSubWindow(musicRoot, musicBackZone);
                break;
            case StartZoneAction.SettingsSound:
                ShowSettingsSubWindow(soundRoot, soundBackZone);
                break;
            case StartZoneAction.SettingsLanguage:
                ShowSettingsSubWindow(languageRoot, languageBackZone);
                break;
            case StartZoneAction.SettingsResolution:
                LogAvailableResolutions();
                ShowResolutionWindow();
                break;
            case StartZoneAction.SettingsBack:
                // Возврат туда, откуда открыли настройки: в паузу или в главное меню.
                if (settingsFromPause)
                {
                    settingsFromPause = false;
                    ShowPauseWindow();
                }
                else
                {
                    ShowStartZones();
                }
                break;
            case StartZoneAction.SettingsWindowBack:
                ShowSettingsZones();
                break;
            case StartZoneAction.Language:
                ApplyLanguage(entry.zoneIndex);
                break;
            case StartZoneAction.VolumeMusic:
                SetVolumeFromClick(true, angle);
                break;
            case StartZoneAction.VolumeSound:
                SetVolumeFromClick(false, angle);
                break;
            case StartZoneAction.Resolution:
                ApplyResolution(entry.zoneIndex);
                break;
        }

        return true;
    }

    /// <summary>
    /// Клик в состоянии Finished: ждём попадания в зону MENU.
    /// </summary>
    private void OnFinishedTap()
    {
        if (activeStartZones.Count == 0)
            return;

        if (TryGetPointerAngle(out float pointerAngle) && TryActivateStartZone(pointerAngle))
            return;

        float arrowAngle = arrowController != null ? arrowController.ArrowAngle : 0f;
        TryActivateStartZone(arrowAngle);
    }

    /// <summary>
    /// Клик в паузе: работают только зоны окна паузы (и окна настроек, если оно открыто).
    /// </summary>
    private void OnPauseTap()
    {
        if (activeStartZones.Count == 0)
            return;

        if (TryGetPointerAngle(out float pointerAngle) && TryActivateStartZone(pointerAngle))
            return;

        float arrowAngle = arrowController != null ? arrowController.ArrowAngle : 0f;
        TryActivateStartZone(arrowAngle);
    }

    // ---------------------------------------------------------------- pause

    /// <summary>
    /// Esc во время боя: замораживаем бой (враги, зоны, таймер, стрелка) и открываем Window_pause.
    /// </summary>
    private void OpenPauseWindow()
    {
        if (paused || state != State.Fighting)
            return;

        paused = true;
        pauseTransitionInProgress = false;

        // Time.timeScale НЕ обнуляем: от него зависит вращение стрелки, иначе из паузы не выйти.
        // Бой замораживается вручную: зоны (движение/сжатие) и враги (твины DOTween).
        slowMoActive = false;
        Time.timeScale = 1f;

        if (zoneManager != null)
            zoneManager.Paused = true;

        DOTween.PauseAll();

        // В паузе стрелка крутится с меню-скоростью, без учёта прокачки.
        if (arrowController != null)
            arrowController.StartRotation(IdleArrowSpeed);

        // Таймер и здоровье врагов на паузе не показываем.
        SetActive(timerRoot, false);
        SetActive(healthRoot, false);

        // Враги и зоны на кольце скрываются, чтобы не мешали окну паузы.
        SetCombatVisualsVisible(false);

        ShowPauseWindow();
    }

    /// <summary>
    /// Показывает окно паузы и регистрирует его зоны (RESUME / MENU / SETTINGS).
    /// </summary>
    private void ShowPauseWindow()
    {
        SwitchZones(ShowPauseWindowNow);
    }

    private void ShowPauseWindowNow()
    {
        activeStartZones.Clear();

        // Открылось меню — подложка темнеет, фон обесцвечивается.
        SetBackgroundMenu(true);

        HideUpgradeWindow();
        HideSettingsWindows();
        HideMenuZones();

        SetWindowActive(pauseWindow, true);
        SetActive(pauseLabel, true);

        AddStartZone(pauseResumeZone, StartZoneAction.PauseResume);
        AddStartZone(pauseMenuZone, StartZoneAction.PauseMenu);
        AddStartZone(pauseSettingsZone, StartZoneAction.PauseSettings);
    }

    /// <summary>
    /// Выход в главное меню из паузы: забег НЕ сбрасывается (прогресс сохраняется,
    /// в меню можно продолжить через CONTINUE). Зоны паузы сначала гаснут анимацией.
    /// </summary>
    private void ExitRunToMenu()
    {
        if (pauseTransitionInProgress)
            return;

        pauseTransitionInProgress = true;

        HideZonesAndWindow(pauseWindow, () =>
        {
            pauseTransitionInProgress = false;
            SetActive(pauseLabel, false);
            ExitRunToMenuNow();
        });
    }

    private void ExitRunToMenuNow()
    {
        paused = false;
        settingsFromPause = false;
        Time.timeScale = 1f;

        if (zoneManager != null)
            zoneManager.Paused = false;

        DOTween.PlayAll();
        HidePauseWindow();

        ClearEnemies();
        ClearLingeringEnemies();
        UpdateEnemyHealthUI();
        zoneManager.StopRound();

        SetZoneActive(mainMenuZone, false);
        SetActive(levelWinObject, false);
        SetActive(levelLoseObject, false);
        SetWindowActive(endWindow, false);

        logLines.Clear();
        if (levelInfoText != null)
            levelInfoText.text = string.Empty;

        // Прогресс (золото, HP, энергия, уровень) остаётся как был.
        SaveProgress();

        // Idle + зоны главного меню (PLAY / CONTINUE / EXIT / SETTINGS).
        PrepareRound(true);
        UpdateGoldUI();
    }

    /// <summary>
    /// Скрывает/показывает врагов и зоны боя (для паузы).
    /// </summary>
    private void SetCombatVisualsVisible(bool visible)
    {
        if (zoneManager != null)
            zoneManager.SetZonesVisible(visible);

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyView enemy = enemies[i];

            if (enemy != null)
                enemy.gameObject.SetActive(visible);
        }
    }

    private void HidePauseWindow()
    {
        SetWindowActive(pauseWindow, false);
        SetActive(pauseLabel, false);
    }

    /// <summary>
    /// Снимает паузу: возвращает Time.timeScale, таймер и обычное управление.
    /// </summary>
    private void ResumeGame()
    {
        if (!paused || pauseTransitionInProgress)
            return;

        pauseTransitionInProgress = true;
        settingsFromPause = false;

        // Сначала гаснут зоны окна паузы, и только потом оно закрывается и бой продолжается.
        HideZonesAndWindow(pauseWindow, () =>
        {
            pauseTransitionInProgress = false;
            SetActive(pauseLabel, false);
            CompleteResume();
        });
    }

    private void CompleteResume()
    {
        if (!paused)
            return;

        paused = false;

        // Снова бой — подложка уходит, фон становится цветным.
        SetBackgroundMenu(false);

        // Возвращаем врагов и зоны боя, продолжаем их анимации.
        SetCombatVisualsVisible(true);

        if (zoneManager != null)
            zoneManager.Paused = false;

        DOTween.PlayAll();

        Time.timeScale = 1f;

        // Возвращаем боевую скорость стрелки (с учётом прокачки) и боевой UI.
        if (state == State.Fighting)
        {
            if (arrowController != null)
                arrowController.StartRotation(GetConfig(roundIndex).arrowSpeed);

            SetActive(timerRoot, true);
            SetActive(healthRoot, true);
        }
    }

    /// <summary>
    /// Угол курсора относительно центра кольца (0° — вверх, по часовой стрелке).
    /// </summary>
    private bool TryGetPointerAngle(out float angle)
    {
        angle = 0f;

        RectTransform ring = ringTransform as RectTransform;
        if (ring == null)
            return false;

        Camera cam = null;
        Canvas canvas = ring.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = canvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(ring, Input.mousePosition, cam, out Vector2 local))
            return false;

        // Клик должен быть рядом с кольцом, иначе любой клик по экрану считался бы попаданием в сектор.
        // Радиус можно превысить: надписи зон (Cell_) выступают за пределы RectTransform кольца.
        float maxRadius = Mathf.Max(ring.rect.width, ring.rect.height) * 0.5f * Mathf.Max(1f, pointerRadiusFactor);

        if (local.magnitude > maxRadius)
            return false;

        if (local.sqrMagnitude < 1f)
            return false;

        angle = NormalizeAngle(Mathf.Atan2(local.x, local.y) * Mathf.Rad2Deg);
        return true;
    }

    /// <summary>Забег начат и ещё не завершён — показываем зону CONTINUE.</summary>
    private bool HasRunInProgress => runStarted;

    private void MarkRunStarted()
    {
        if (runStarted)
            return;

        runStarted = true;
        FBPP.SetBool(SaveRunStarted, true);
    }

    private void MarkRunFinished()
    {
        runStarted = false;
        FBPP.SetBool(SaveRunStarted, false);
    }

    /// <summary>
    /// Новая игра: сбрасываем прогресс забега (раунд, жизни, энергия).
    /// Золото и прокачка сохраняются, купленные релики сбрасываются.
    /// </summary>
    private void StartNewRun()
    {
        roundIndex = 0;
        heroHp = maxHeroHp;
        energy = CurrentMaxEnergy;

        // Новая игра — золото сбрасывается вместе с остальным прогрессом забега.
        gold = 0;

        // Новая игра — купленные релики удаляются.
        ResetRelicLevels();

        MarkRunFinished();

        FBPP.SetInt(SaveGold, gold);
        FBPP.SetInt(SaveHeroHp, maxHeroHp);
        FBPP.SetInt(SaveRound, 0);
        FBPP.SetFloat(SaveEnergy, energy);
        FBPP.SetBool(SaveWin, false);

        UpdateGoldUI();
        UpdateLifeUI();
        UpdateEnergyUI();
        UpdateRoundUI();

        StartBattle();
    }

    private void StartBattle()
    {
        RoundConfig config = GetConfig(roundIndex);
        state = State.Fighting;
        timeLeft = config.roundDuration + RelicRoundTimeBonus;

        // Новый бой стартует без паузы.
        paused = false;
        settingsFromPause = false;
        HidePauseWindow();

        // Никаких отложенных обновлений UI с прошлого боя.
        suppressLifeUi = false;
        suppressEnergyUi = false;
        suppressGoldUi = false;
        suppressTimerUi = false;
        waitingDamageFlash = false;
        lastTimerTenths = -1;

        // Серия кликов и кровотечение начинаются заново.
        relicStreakHits = 0;
        relicBleedAccumulator = 0f;
        timerFailInProgress = false;

        if (arrowController != null)
            arrowController.SpeedMultiplier = 1f;

        MarkRunStarted();
        // Зоны раунда появятся и таймер пойдёт только через battleStartDelay секунд.
        roundStarting = battleStartDelay > 0.01f;
        startDelayLeft = roundStarting ? battleStartDelay : 0f;

        HideStartZones();

        SetBattleUiVisible(true);
        SetInGameHud(true);
        SetRoundLabelVisible(true);

        // Забег начался — показываем золото.
        SetActive(goldRoot, true);
        UpdateGoldUI();

        // Info_relics: на старте уровня только если куплено больше одного релика.
        UpdateInfoRelicsVisibility();

        // Сначала подменяем фон (пока он ещё серый), потом он плавно наливается цветом.
        ApplyRoundBackground();

        // В бою чёрная подложка уходит, а фон становится цветным.
        SetBackgroundMenu(false);

        // Новые враги появляются только когда старые полностью растворятся (FadeAmount).
        ClearEnemiesAndLingering(() =>
        {
            SpawnEnemies(roundIndex);
            UpdateEnemyHealthUI();
        });

        // Новый забег — витрина магазина создаётся заново.
        RerollShopRelics();

        UpdateTimerUI();

        arrowController.StartRotation(config.arrowSpeed);

        if (!roundStarting)
            zoneManager.BeginRound(config.zones);
    }

    /// <summary>
    /// Создаёт зоны текущего раунда (вызывается по окончании задержки старта боя).
    /// </summary>
    private void BeginRoundZones()
    {
        RoundConfig config = GetConfig(roundIndex);
        timeLeft = config.roundDuration + RelicRoundTimeBonus;
        UpdateTimerUI();

        arrowController.StartRotation(config.arrowSpeed);
        zoneManager.BeginRound(config.zones);
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ShowStartZones()
    {
        SwitchZones(ShowStartZonesNow);
    }

    private void ShowStartZonesNow()
    {
        activeStartZones.Clear();

        // Главное меню — подложка темнеет, фон обесцвечивается, боевой HUD скрывается.
        SetBackgroundMenu(true);
        SetInGameHud(false);

        // В главном меню — зона "Играть".
        HideUpgradeWindow();
        HideSettingsWindows();
        HidePauseWindow();
        SetWindowActive(endWindow, false);
        HideMenuZones();

        // В главном меню Info_relics скрыта (вернётся на старте уровня).
        SetInfoRelicsVisible(false);

        AddStartZone(playZone, StartZoneAction.NewGame);
        AddStartZone(exitZone, StartZoneAction.Exit);
        AddStartZone(settingsZone, StartZoneAction.Settings);

        // CONTINUE виден только если забег начат и не проигран/не завершён.
        if (HasRunInProgress)
            AddStartZone(continueZone, StartZoneAction.Continue);
        else
            SetZoneActive(continueZone, false);

        // В меню стрелка всегда крутится со своей скоростью, независимо от прокачки.
        if (arrowController != null)
            arrowController.StartRotation(IdleArrowSpeed);
    }

    /// <summary>
    /// Окно настроек: зоны меню гасятся, включаются зоны Window_settings.
    /// </summary>
    private void ShowSettingsZones()
    {
        SwitchZones(ShowSettingsZonesNow);
    }

    private void ShowSettingsZonesNow()
    {
        activeStartZones.Clear();

        HideUpgradeWindow();
        HideMenuZones();
        HideSettingsWindows();

        if (settingsRoot != null && !settingsRoot.gameObject.activeSelf)
            settingsRoot.gameObject.SetActive(true);

        AddStartZone(settingsMusicZone, StartZoneAction.SettingsMusic);
        AddStartZone(settingsSoundZone, StartZoneAction.SettingsSound);
        AddStartZone(settingsLanguageZone, StartZoneAction.SettingsLanguage);
        AddStartZone(settingsResolutionZone, StartZoneAction.SettingsResolution);
        AddStartZone(settingsBackZone, StartZoneAction.SettingsBack);
    }

    /// <summary>
    /// Подокно настроек: все текущие зоны гасятся, включаются зоны выбранного окна.
    /// </summary>
    private void ShowSettingsSubWindow(Transform root, Transform back)
    {
        SwitchZones(() => ShowSettingsSubWindowNow(root, back));
    }

    private void ShowSettingsSubWindowNow(Transform root, Transform back)
    {
        activeStartZones.Clear();

        HideUpgradeWindow();
        HideMenuZones();
        HideSettingsWindows();

        if (root == null)
            return;

        if (!root.gameObject.activeSelf)
            root.gameObject.SetActive(true);

        // Все зоны подокна подсвечиваются при наведении;
        // зоны выбора языка получают действие Language, слайдеры громкости — VolumeMusic / VolumeSound.
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            if (child == back)
                continue;

            int languageIndex = IndexOfLanguageZone(child);

            if (languageIndex >= 0)
            {
                AddStartZone(child, StartZoneAction.Language, languageIndex);
                continue;
            }

            if (child == musicVolumeBackZone || child == musicVolumeZone)
            {
                // Клик считается по Back-зоне, а fill меняется у основной зоны.
                AddStartZone(child, child == musicVolumeBackZone ? StartZoneAction.VolumeMusic : StartZoneAction.None);
                continue;
            }

            if (child == soundVolumeBackZone || child == soundVolumeZone)
            {
                AddStartZone(child, child == soundVolumeBackZone ? StartZoneAction.VolumeSound : StartZoneAction.None);
                continue;
            }

            AddStartZone(child, StartZoneAction.None);
        }

        UpdateVolumeZones();

        AddStartZone(back, StartZoneAction.SettingsWindowBack);
    }

    /// <summary>
    /// Гасит все зоны меню (PLAY / CONTINUE / EXIT / SETTINGS / UPDATES / SHOP / MENU / FIGHT).
    /// </summary>
    private void HideMenuZones()
    {
        SetZoneActive(playZone, false);
        SetZoneActive(continueZone, false);
        SetZoneActive(fightZone, false);
        SetZoneActive(exitZone, false);
        SetZoneActive(settingsZone, false);
        SetZoneActive(updatesZone, false);
        SetZoneActive(shopZone, false);
        SetZoneActive(mainMenuZone, false);
    }

    /// <summary>
    /// Готовит зоны всех окон настроек (добавляет ZoneFade и кеширует параметры).
    /// </summary>
    private void PrepareSettingsWindows()
    {
        PrepareZoneFade(settingsMusicZone);
        PrepareZoneFade(settingsSoundZone);
        PrepareZoneFade(settingsLanguageZone);
        PrepareZoneFade(settingsResolutionZone);
        PrepareZoneFade(settingsBackZone);

        PrepareZoneFade(musicBackZone);
        PrepareZoneFade(soundBackZone);
        PrepareZoneFade(languageBackZone);
        PrepareZoneFade(resolutionBackZone);

        PrepareWindowZones(musicRoot, musicBackZone);
        PrepareWindowZones(soundRoot, soundBackZone);
        PrepareWindowZones(languageRoot, languageBackZone);
        PrepareWindowZones(resolutionRoot, resolutionBackZone);

        PrepareLanguageZones();
        PrepareNoHoverFillZones();
    }

    /// <summary>
    /// Зоны, у которых при наведении fill не меняется (значение музыки/звука).
    /// </summary>
    private void PrepareNoHoverFillZones()
    {
        if (noHoverFillZones == null)
            return;

        for (int i = 0; i < noHoverFillZones.Length; i++)
        {
            ZoneFade fade = PrepareZoneFade(noHoverFillZones[i]);

            if (fade != null)
                fade.hoverFillEnabled = false;
        }
    }

    private void PrepareWindowZones(Transform root, Transform back)
    {
        PrepareZoneFade(back);

        if (root == null)
            return;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            if (child == back)
                continue;

            PrepareZoneFade(child);

            // Надписи внутри подокон — числа/значения, их не переводим.
            DisableTranslate(child);
        }
    }

    /// <summary>
    /// Названия языков всегда пишем на самом языке — автоперевод отключаем.
    /// </summary>
    private void PrepareLanguageZones()
    {
        if (languageZones == null)
            return;

        for (int i = 0; i < languageZones.Length; i++)
        {
            Transform zone = languageZones[i];

            if (zone == null)
                continue;

            ZoneFade fade = PrepareZoneFade(zone);
            TMP_Text label = fade != null ? fade.Label : null;

            if (label != null && i < LanguageNativeNames.Length)
                label.text = LanguageNativeNames[i];
        }
    }

    private static void DisableTranslate(Transform zone)
    {
        if (zone == null)
            return;

        Translate[] translates = zone.GetComponentsInChildren<Translate>(true);

        for (int i = 0; i < translates.Length; i++)
        {
            if (translates[i] != null)
                translates[i].enabled = false;
        }
    }

    /// <summary>
    /// Прячет окно настроек и все его подокна.
    /// </summary>
    private void HideSettingsWindows()
    {
        HideSettingsZone(settingsMusicZone);
        HideSettingsZone(settingsSoundZone);
        HideSettingsZone(settingsLanguageZone);
        HideSettingsZone(settingsResolutionZone);
        HideSettingsZone(settingsBackZone);

        HideSettingsWindow(musicRoot, musicBackZone);
        HideSettingsWindow(soundRoot, soundBackZone);
        HideSettingsWindow(languageRoot, languageBackZone);
        HideSettingsWindow(resolutionRoot, resolutionBackZone);

        if (settingsRoot != null && settingsRoot.gameObject.activeSelf)
            settingsRoot.gameObject.SetActive(false);
    }

    private void HideSettingsWindow(Transform root, Transform back)
    {
        HideSettingsZone(back);

        if (root != null)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);

                if (child != back)
                    HideSettingsZone(child);
            }

            if (root.gameObject.activeSelf)
                root.gameObject.SetActive(false);
        }
    }

    private static void HideSettingsZone(Transform zone)
    {
        if (zone == null)
            return;

        ZoneFade fade = zone.GetComponent<ZoneFade>();

        if (fade != null)
            fade.Hide(true);
        else
            zone.gameObject.SetActive(false);
    }

    /// <summary>
    /// Индекс зоны языка в languageZones (или -1, если это не зона языка).
    /// </summary>
    private int IndexOfLanguageZone(Transform zone)
    {
        if (zone == null || languageZones == null)
            return -1;

        for (int i = 0; i < languageZones.Length; i++)
        {
            if (languageZones[i] == zone)
                return i;
        }

        return -1;
    }

    /// <summary>
    /// Переключает язык: переводит все тексты в сцене и запоминает выбор.
    /// </summary>
    private void ApplyLanguage(int index)
    {
        if (index < 0 || index >= LanguageNames.Length)
            return;

        string language = LanguageNames[index];

        LocalizationManager.Language = language;
        FBPP.SetString(SaveLanguage, language);

        // Переводим весь текст на сцене.
        Translate.RefreshAll();

        // Тексты, которые GameManager пишет сам: переводим и меняем шрифт под язык.
        UpdateUpgradePrices();
        UpdateShopPrices();

        // Подсказку Info_relics пересоберём на новом языке.
        infoRelicsTextDirty = true;
        UpdateRoundUI();
        UpdateGoldUI();

        ApplyUiFonts();
    }

    /// <summary>
    /// Ставит шрифт по выбранному языку на надписи, которые пишет сам GameManager.
    /// </summary>
    private void ApplyUiFonts()
    {
        Translate.ApplyFont(roundText);
        Translate.ApplyFont(timerText);
        Translate.ApplyFont(goldText);
        Translate.ApplyFont(lifeText);
        Translate.ApplyFont(healthText);
        Translate.ApplyFont(energyText);
        Translate.ApplyFont(levelInfoText);
    }

    /// <summary>
    /// Разрешения, которые подходят игре: соотношение сторон и герцовка совпадают с текущими.
    /// </summary>
    private List<Resolution> GetAvailableResolutions()
    {
        List<Resolution> result = new List<Resolution>();

        float currentAspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 0f;
        float currentHz = (float)Screen.currentResolution.refreshRateRatio.value;

        // Допуски: на округления вида 1919x1080 / 2559x1440 и на 59.94 / 143.98 Гц.
        const float AspectTolerance = 0.01f;
        const float HzTolerance = 0.5f;

        Resolution[] all = Screen.resolutions;

        for (int i = 0; i < all.Length; i++)
        {
            Resolution resolution = all[i];

            if (resolution.height <= 0)
                continue;

            float aspect = (float)resolution.width / resolution.height;

            if (Mathf.Abs(aspect - currentAspect) > currentAspect * AspectTolerance)
                continue;

            float hz = (float)resolution.refreshRateRatio.value;

            if (Mathf.Abs(hz - currentHz) > HzTolerance)
                continue;

            bool duplicate = false;

            for (int j = 0; j < result.Count; j++)
            {
                if (result[j].width == resolution.width && result[j].height == resolution.height)
                {
                    duplicate = true;
                    break;
                }
            }

            if (!duplicate)
                result.Add(resolution);
        }

        return result;
    }

    /// <summary>
    /// Печатает в консоль разрешения экрана, у которых совпадают и соотношение сторон, и герцовка.
    /// </summary>
    private void LogAvailableResolutions()
    {
        float currentAspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 0f;
        float currentHz = (float)Screen.currentResolution.refreshRateRatio.value;

        Debug.Log(string.Format(
            "[Resolution] Текущее: {0} x {1} (соотношение {2}, {3} Гц). Всего вариантов: {4}",
            Screen.width,
            Screen.height,
            currentAspect.ToString("0.###"),
            currentHz.ToString("0.##"),
            Screen.resolutions.Length));

        List<Resolution> matching = GetAvailableResolutions();

        if (matching.Count == 0)
        {
            Debug.Log(string.Format(
                "[Resolution] Нет вариантов с соотношением {0} и герцовкой {1} Гц — остаётся только текущее разрешение.",
                currentAspect.ToString("0.###"),
                currentHz.ToString("0.##")));
            return;
        }

        Debug.Log(string.Format(
            "[Resolution] Подходит (соотношение {0}, {1} Гц): {2}",
            currentAspect.ToString("0.###"),
            currentHz.ToString("0.##"),
            matching.Count));

        List<string> unique = new List<string>();

        for (int i = 0; i < matching.Count; i++)
        {
            Resolution resolution = matching[i];

            Debug.Log(string.Format(
                "[Resolution] {0}) {1} x {2} @ {3} Гц (соотношение {4})",
                i + 1,
                resolution.width,
                resolution.height,
                resolution.refreshRateRatio.value.ToString("0.##"),
                ((float)resolution.width / resolution.height).ToString("0.###")));

            string size = resolution.width + "x" + resolution.height;

            if (!unique.Contains(size))
                unique.Add(size);
        }

        Debug.Log("[Resolution] Уникальные размеры: " + string.Join(", ", unique.ToArray()));
    }

    /// <summary>
    /// Окно разрешения: включаются только те ZoneResolution, для которых есть подходящее разрешение
    /// (не больше 11; если вариантов больше — берём последние).
    /// </summary>
    private void ShowResolutionWindow()
    {
        SwitchZones(ShowResolutionWindowNow);
    }

    private void ShowResolutionWindowNow()
    {
        activeStartZones.Clear();

        HideUpgradeWindow();
        HideMenuZones();
        HideSettingsWindows();

        if (resolutionRoot == null)
            return;

        if (!resolutionRoot.gameObject.activeSelf)
            resolutionRoot.gameObject.SetActive(true);

        List<Resolution> available = GetAvailableResolutions();

        int zoneCount = 0;

        for (int i = 0; i < resolutionRoot.childCount; i++)
        {
            if (resolutionRoot.GetChild(i) != resolutionBackZone)
                zoneCount++;
        }

        int used = Mathf.Min(available.Count, zoneCount);
        int firstIndex = Mathf.Max(0, available.Count - used);
        int index = 0;

        shownResolutions.Clear();
        appliedResolutionIndex = -1;

        // BACK всегда снизу, а зоны разрешений делят с ним окружность равномерно.
        if (resolutionBackZone != null)
            resolutionBackZone.localRotation = Quaternion.Euler(0f, 0f, 180f);

        for (int i = 0; i < resolutionRoot.childCount; i++)
        {
            Transform child = resolutionRoot.GetChild(i);

            if (child == resolutionBackZone)
                continue;

            if (index < used)
            {
                Resolution resolution = available[firstIndex + index];

                ApplyResolutionZone(child, resolution, index, used);
                shownResolutions.Add(resolution);
                AddStartZone(child, StartZoneAction.Resolution, shownResolutions.Count - 1);
                index++;
            }
            else
            {
                // Лишние зоны не показываем.
                HideSettingsZone(child);
                child.gameObject.SetActive(false);
            }
        }

        AddStartZone(resolutionBackZone, StartZoneAction.SettingsWindowBack);

        // Текущим считаем сохранённый выбор (Screen в редакторе может не обновиться сразу).
        appliedResolutionIndex = FindSavedResolutionIndex();
        UpdateResolutionSelection();
    }

    /// <summary>
    /// Индекс сохранённого (или совпадающего с экраном) разрешения в shownResolutions.
    /// </summary>
    private int FindSavedResolutionIndex()
    {
        int width = FBPP.GetInt(SaveResolutionWidth, 0);
        int height = FBPP.GetInt(SaveResolutionHeight, 0);

        if (width > 0 && height > 0)
        {
            for (int i = 0; i < shownResolutions.Count; i++)
            {
                if (shownResolutions[i].width == width && shownResolutions[i].height == height)
                    return i;
            }
        }

        for (int i = 0; i < shownResolutions.Count; i++)
        {
            if (IsCurrentResolution(shownResolutions[i]))
                return i;
        }

        return -1;
    }

    /// <summary>
    /// Применяет выбранное разрешение и подсвечивает его в списке.
    /// </summary>
    private void ApplyResolution(int index)
    {
        if (index < 0 || index >= shownResolutions.Count)
            return;

        Resolution resolution = shownResolutions[index];

        Screen.SetResolution(
            resolution.width,
            resolution.height,
            Screen.fullScreenMode,
            resolution.refreshRateRatio);

        Debug.Log(string.Format(
            "[Resolution] Применено: {0} x {1} @ {2} Гц",
            resolution.width,
            resolution.height,
            resolution.refreshRateRatio.value.ToString("0.##")));

        appliedResolutionIndex = index;
        SaveResolution(resolution);
        UpdateResolutionSelection();
    }

    /// <summary>
    /// Красит надпись зоны, соответствующей текущему разрешению.
    /// </summary>
    private void UpdateResolutionSelection()
    {
        for (int i = 0; i < shownResolutions.Count; i++)
        {
            Transform zone = FindResolutionZoneForIndex(i);

            if (zone == null)
                continue;

            ZoneFade fade = PrepareZoneFade(zone);

            bool selected = appliedResolutionIndex >= 0
                ? i == appliedResolutionIndex
                : IsCurrentResolution(shownResolutions[i]);

            if (fade != null)
                fade.SetSelected(selected);
        }
    }

    /// <summary>
    /// Зона, которой назначено разрешение с индексом (по порядку детей окна без BACK).
    /// </summary>
    private Transform FindResolutionZoneForIndex(int index)
    {
        if (resolutionRoot == null || index < 0)
            return null;

        int current = 0;

        for (int i = 0; i < resolutionRoot.childCount; i++)
        {
            Transform child = resolutionRoot.GetChild(i);

            if (child == resolutionBackZone)
                continue;

            if (current == index)
                return child;

            current++;
        }

        return null;
    }

    private static bool IsCurrentResolution(Resolution resolution)
    {
        if (Screen.width != resolution.width || Screen.height != resolution.height)
            return false;

        float hz = (float)resolution.refreshRateRatio.value;
        float currentHz = (float)Screen.currentResolution.refreshRateRatio.value;

        return Mathf.Abs(hz - currentHz) <= 0.5f;
    }

    /// <summary>
    /// Запоминает выбранное разрешение.
    /// </summary>
    private static void SaveResolution(Resolution resolution)
    {
        FBPP.SetInt(SaveResolutionWidth, resolution.width);
        FBPP.SetInt(SaveResolutionHeight, resolution.height);
        FBPP.SetInt(SaveResolutionHzNumerator, (int)resolution.refreshRateRatio.numerator);
        FBPP.SetInt(SaveResolutionHzDenominator, (int)resolution.refreshRateRatio.denominator);
    }

    /// <summary>
    /// Применяет сохранённое разрешение при запуске игры.
    /// </summary>
    private void ApplySavedResolution()
    {
        int width = FBPP.GetInt(SaveResolutionWidth, 0);
        int height = FBPP.GetInt(SaveResolutionHeight, 0);

        if (width <= 0 || height <= 0)
            return;

        if (width == Screen.width && height == Screen.height)
            return;

        int numerator = FBPP.GetInt(SaveResolutionHzNumerator, 0);
        int denominator = FBPP.GetInt(SaveResolutionHzDenominator, 0);

        if (numerator > 0 && denominator > 0)
        {
            RefreshRate refreshRate = new RefreshRate
            {
                numerator = (uint)numerator,
                denominator = (uint)denominator
            };

            Screen.SetResolution(width, height, Screen.fullScreenMode, refreshRate);
        }
        else
        {
            Screen.SetResolution(width, height, Screen.fullScreenMode);
        }

        Debug.Log(string.Format("[Resolution] Применено сохранённое: {0} x {1}", width, height));
    }

    /// <summary>
    /// Раскладывает зону разрешения по кругу и пишет разрешение в надпись.
    /// При 11 зонах шаг 30° и fill 0.07; при другом количестве — пропорционально (360/(N+1)).
    /// </summary>
    private static void ApplyResolutionZone(Transform zone, Resolution resolution, int index, int total)
    {
        ZoneFade fade = zone != null ? zone.GetComponent<ZoneFade>() : null;

        if (fade == null)
            return;

        float step = 360f / (total + 1);
        float angle = 180f + step * (index + 1);

        zone.localRotation = Quaternion.Euler(0f, 0f, angle);

        // Ширина сектора пропорциональна шагу: 30° -> 0.07.
        fade.targetFill = Mathf.Clamp(0.07f * (step / 30f), 0.001f, 1f);
        fade.SetFillScale(1f, true);

        if (fade.Label == null)
            return;

        fade.Label.text = resolution.width + "x" + resolution.height;

        // Надпись разворачиваем по половине в локальных градусах: сверху — 0°, снизу — 180°.
        float labelRotation = Mathf.Cos(angle * Mathf.Deg2Rad) >= 0f ? 0f : 180f;
        fade.Label.rectTransform.localRotation = Quaternion.Euler(0f, 0f, labelRotation);
    }

    // ---------------------------------------------------------------- volume

    /// <summary>
    /// Клик по слайдеру громкости: сектор основной зоны дотягивается до места, где была стрелка.
    /// </summary>
    private void SetVolumeFromClick(bool music, float clickAngle)
    {
        Transform zone = music ? musicVolumeZone : soundVolumeZone;

        if (zone == null)
            return;

        ZoneFade fade = PrepareZoneFade(zone);

        if (fade == null)
            return;

        float rotation = NormalizeAngle(zone.eulerAngles.z + startZoneAngleOffset);
        float maxFill = Mathf.Max(0.001f, fade.MaxFill);
        float value;

        if (!fade.SymmetricFill)
        {
            // Односторонний сектор: он растёт от поворота зоны, поэтому fill
            // подбирается так, чтобы его край оказался ровно под стрелкой.
            float arc = invertVolumeDirection
                ? Mathf.Repeat(clickAngle - rotation, 360f)
                : Mathf.Repeat(rotation - clickAngle, 360f);

            value = Mathf.Clamp01(arc / (maxFill * 360f));
        }
        else
        {
            // Симметричный сектор: растёт от центра к краям.
            float half = Mathf.Max(1f, maxFill * 180f);
            float delta = Mathf.DeltaAngle(rotation, clickAngle);

            value = volumeFromCenter
                ? Mathf.Clamp01(Mathf.Abs(delta) / half)
                : Mathf.Clamp01(0.5f + delta / (half * 2f));
        }

        SetVolume(music, value);
    }

    private void SetVolume(bool music, float value)
    {
        value = Mathf.Clamp01(value);

        if (music)
            musicVolume = value;
        else
            soundVolume = value;

        SaveVolumes();
        UpdateVolumeZones();
    }

    /// <summary>
    /// Длина сектора = доля громкости, в надписи — процент от максимума.
    /// </summary>
    private void UpdateVolumeZones()
    {
        ApplyVolumeZone(musicVolumeZone, musicVolume);
        ApplyVolumeZone(musicVolumeBackZone, musicVolume);
        ApplyVolumeZone(soundVolumeZone, soundVolume);
        ApplyVolumeZone(soundVolumeBackZone, soundVolume);
    }

    private void ApplyVolumeZone(Transform zone, float volume)
    {
        if (zone == null)
            return;

        ZoneFade fade = PrepareZoneFade(zone);

        if (fade == null)
            return;

        // Парная зона (Back) остаётся полной — меняется только надпись.
        if (zone == musicVolumeZone || zone == soundVolumeZone)
            fade.SetFillScale(volume);

        WriteVolumePercent(fade.Label, volume);
    }

    private static void WriteVolumePercent(TMP_Text label, float volume)
    {
        if (label == null)
            return;

        label.text = Mathf.RoundToInt(Mathf.Clamp01(volume) * 100f) + "%";
    }

    private void SaveVolumes()
    {
        FBPP.SetFloat(SaveMusicVolume, musicVolume);
        FBPP.SetFloat(SaveSoundVolume, soundVolume);
    }

    private void LoadVolumes()
    {
        musicVolume = Mathf.Clamp01(FBPP.GetFloat(SaveMusicVolume, musicVolume));
        soundVolume = Mathf.Clamp01(FBPP.GetFloat(SaveSoundVolume, soundVolume));
    }

    /// <summary>
    /// Окно прокачки: зоны меню гасятся, включается Window_update и зона BACK.
    /// </summary>
    private void ShowUpgradeWindow()
    {
        SwitchZones(ShowUpgradeWindowNow);
    }

    private void ShowUpgradeWindowNow()
    {
        activeStartZones.Clear();

        HideSettingsWindows();
        SetZoneActive(playZone, false);
        SetZoneActive(continueZone, false);
        SetZoneActive(fightZone, false);
        SetZoneActive(exitZone, false);
        SetZoneActive(settingsZone, false);
        SetZoneActive(updatesZone, false);
        SetZoneActive(shopZone, false);
        SetZoneActive(mainMenuZone, false);
        SetWindowActive(afterFightWindow, false);
        HideShopWindow();

        if (upgradeRoot != null && !upgradeRoot.gameObject.activeSelf)
            upgradeRoot.gameObject.SetActive(true);

        EnsureUpgrades();

        // Все зоны прокачки берутся из таблицы upgrades (без поиска объектов в сцене).
        for (int i = 0; i < upgrades.Length; i++)
        {
            UpgradeInfo info = upgrades[i];

            if (info == null || info.zone == null)
                continue;

            // Текст зоны и ценник пишет GameManager — автоперевод отключаем,
            // иначе Translate перезапишет их с задержкой в кадр.
            DisableTranslate(info.zone);

            AddStartZone(info.zone, StartZoneAction.Upgrade, i);
        }

        AddStartZone(backZone, StartZoneAction.Back);

        UpdateUpgradePrices();
    }

    /// <summary>
    /// Цена в Cell (альфа 0.5, если золота не хватает) и надпись зоны с уровнем.
    /// </summary>
    private void UpdateUpgradePrices()
    {
        EnsureUpgrades();

        for (int i = 0; i < upgrades.Length; i++)
        {
            UpgradeInfo info = upgrades[i];

            if (info == null)
                continue;

            if (info.priceText != null)
            {
                // Цена или "max".
                info.priceText.text = info.IsMaxLevel ? MaxUpgradePriceLabel : info.NextPrice.ToString();

                if (!info.priceColorCached)
                {
                    info.basePriceColor = info.priceText.color;
                    info.priceColorCached = true;
                }

                // Цвет ценника по состоянию: максимум — жёлтый, не хватает золота — красный.
                if (info.IsMaxLevel)
                    info.priceText.color = UpgradeMaxPriceColor;
                else if (info.NextPrice > gold)
                    info.priceText.color = UpgradeUnaffordablePriceColor;
                else
                    info.priceText.color = info.basePriceColor;

                Translate.ApplyFont(info.priceText);
            }

            // Недоступная зона (максимум или не хватает золота) гаснет по альфе и не подсвечивается.
            if (info.zone != null)
            {
                ZoneFade fade = info.zone.GetComponent<ZoneFade>();

                if (fade != null)
                    fade.SetDimmed(info.IsMaxLevel || info.NextPrice > gold);
            }

            UpdateUpgradeLabel(info);
        }
    }

    /// <summary>
    /// Надпись зоны прокачки: "НАЗВАНИЕ (текущий/максимальный)".
    /// </summary>
    private void UpdateUpgradeLabel(UpgradeInfo info)
    {
        if (info == null || info.zone == null)
            return;

        ZoneFade fade = info.zone.GetComponent<ZoneFade>();
        TMP_Text label = fade != null ? fade.Label : null;

        if (label == null)
            return;

        string localizedName = LocalizeOr(info.localizationKey, info.name);
        label.text = string.Format("{0} ({1}/{2})", localizedName, info.level, UpgradeInfo.MaxLevel);
        Translate.ApplyFont(label);
    }

    /// <summary>
    /// Доступна ли прокачка: уровень не максимальный и хватает золота.
    /// </summary>
    private bool IsUpgradeAvailable(int index)
    {
        UpgradeInfo info = GetUpgrade(index);

        return info != null && !info.IsMaxLevel && info.NextPrice <= gold;
    }

    /// <summary>
    /// Покупка одного уровня прокачки: списывает золото и повышает уровень зоны.
    /// Возвращает true, если покупка совершена.
    /// </summary>
    private bool TryBuyUpgrade(int index)
    {
        EnsureUpgrades();

        if (index < 0 || index >= upgrades.Length)
            return false;

        UpgradeInfo info = upgrades[index];

        if (info == null)
            return false;

        if (info.IsMaxLevel)
        {
            Debug.Log(string.Format("[Upgrade] {0}: максимальный уровень", info.name));
            return false;
        }

        int price = info.NextPrice;

        if (price > gold)
        {
            Debug.Log(string.Format("[Upgrade] {0}: не хватает золота (нужно {1}, есть {2})", info.name, price, gold));
            return false;
        }

        gold -= price;

        float energyBefore = CurrentMaxEnergy;

        info.level = Mathf.Clamp(info.level + 1, 0, UpgradeInfo.MaxLevel);

        FBPP.SetInt(SaveGold, gold);
        SaveUpgrades();

        // Прокачка энергии: сразу добавляем прирост к текущей энергии,
        // чтобы купленная длительность слоу-мо стала доступна.
        if (index == UpgradeEnergyIndex)
        {
            float energyAfter = CurrentMaxEnergy;
            energy = Mathf.Min(energyAfter, Mathf.Max(0f, energy) + Mathf.Max(0f, energyAfter - energyBefore));
        }

        // Перерисовывает золото, цены и надписи зон с уровнями.
        UpdateGoldUI();
        UpdateUpgradePrices();

        // Короткое замедление стрелки до 10%, чтобы покупка была заметна.
        if (arrowController != null)
            arrowController.SlowDown(upgradeArrowSlowFactor, upgradeArrowSlowDuration);

        Debug.Log(string.Format("[Upgrade] {0} -> {1}/{2} (цена {3}, золото {4})",
            info.name, info.level, UpgradeInfo.MaxLevel, price, gold));

        return true;
    }

    /// <summary>
    /// Сохраняет уровни прокачки (ключ на каждую зону).
    /// </summary>
    private void SaveUpgrades()
    {
        if (upgrades == null)
            return;

        for (int i = 0; i < upgrades.Length; i++)
        {
            if (upgrades[i] == null)
                continue;

            FBPP.SetInt(SaveUpgradeLevelPrefix + i, upgrades[i].level);
        }
    }

    /// <summary>
    /// Загружает уровни прокачки, если они уже были куплены.
    /// </summary>
    private void LoadUpgrades()
    {
        EnsureUpgrades();

        for (int i = 0; i < upgrades.Length; i++)
        {
            if (upgrades[i] == null)
                continue;

            upgrades[i].level = Mathf.Clamp(
                FBPP.GetInt(SaveUpgradeLevelPrefix + i, upgrades[i].level),
                0,
                UpgradeInfo.MaxLevel);
        }
    }

    private void HideUpgradeWindow()
    {
        if (upgradeRoot != null && upgradeRoot.gameObject.activeSelf)
            upgradeRoot.gameObject.SetActive(false);
    }

    /// <summary>
    /// Окно магазина: зоны между боями гасятся, включается Window_shop и зона BACK.
    /// </summary>
    private void ShowShopWindow()
    {
        SwitchZones(ShowShopWindowNow);
    }

    private void ShowShopWindowNow()
    {
        activeStartZones.Clear();

        HideUpgradeWindow();
        HideSettingsWindows();
        SetZoneActive(playZone, false);
        SetZoneActive(continueZone, false);
        SetZoneActive(fightZone, false);
        SetZoneActive(exitZone, false);
        SetZoneActive(settingsZone, false);
        SetZoneActive(updatesZone, false);
        SetZoneActive(shopZone, false);
        SetZoneActive(mainMenuZone, false);
        SetWindowActive(afterFightWindow, false);

        SetWindowActive(shopWindow, true);

        EnsureShopItems();

        // Декоративные дочерние зоны (не BACK, не HP/MP, не релики).
        if (shopWindow != null)
        {
            for (int i = 0; i < shopWindow.childCount; i++)
            {
                Transform child = shopWindow.GetChild(i);

                if (child == shopBackZone || child == shopHpZone || child == shopEnergyZone)
                    continue;

                if (IsShopItemZone(child))
                    continue;

                AddStartZone(child, StartZoneAction.None);
            }
        }

        // Покупки: HP, MP и три зоны реликов.
        AddStartZone(shopHpZone, StartZoneAction.ShopRestoreHp);
        AddStartZone(shopEnergyZone, StartZoneAction.ShopRestoreEnergy);

        for (int i = 0; i < ShopItemCount; i++)
            AddStartZone(GetShopItemZone(i), StartZoneAction.ShopBuyItem, i);

        AddStartZone(shopBackZone, StartZoneAction.ShopBack);

        UpdateShopPrices();
    }

    private void HideShopWindow()
    {
        SetWindowActive(shopWindow, false);
    }

    /// <summary>
    /// Готовит ценники магазина: числа не переводим, базовый цвет запоминаем.
    /// </summary>
    private void PrepareShopPrices()
    {
        if (shopHpPrice < 0)
            shopHpPrice = 0;

        if (shopEnergyPrice < 0)
            shopEnergyPrice = 0;

        // Ценники — числа, автоперевод им не нужен.
        DisableTranslate(shopHpPriceText != null ? shopHpPriceText.transform : null);
        DisableTranslate(shopEnergyPriceText != null ? shopEnergyPriceText.transform : null);

        if (shopHpPriceText != null)
            shopHpBasePriceColor = shopHpPriceText.color;

        if (shopEnergyPriceText != null)
            shopEnergyBasePriceColor = shopEnergyPriceText.color;

        EnsureShopItems();

        if (shopItemBasePriceColors == null || shopItemBasePriceColors.Length != ShopItemCount)
            shopItemBasePriceColors = new Color[ShopItemCount];

        if (shopItemTitleHomePos == null || shopItemTitleHomePos.Length != ShopItemCount)
            shopItemTitleHomePos = new Vector3[ShopItemCount];

        if (shopItemTitleHomeScale == null || shopItemTitleHomeScale.Length != ShopItemCount)
            shopItemTitleHomeScale = new Vector3[ShopItemCount];

        for (int i = 0; i < ShopItemCount; i++)
        {
            TMP_Text text = GetShopItemPriceText(i);

            if (text != null)
            {
                // Ценники — числа, автоперевод им не нужен.
                DisableTranslate(text.transform);
                shopItemBasePriceColors[i] = text.color;
            }

            // Название пишется с уровнем — автоперевод его бы стёр.
            TMP_Text nameText = GetShopItemNameText(i);
            DisableTranslate(nameText != null ? nameText.transform : null);

            // Описание тоже пишет GameManager (числа под уровень).
            TMP_Text descText = GetShopItemDescText(i);
            DisableTranslate(descText != null ? descText.transform : null);

            // Запоминаем исходное состояние Tittle (для возврата после полёта к Info_relics).
            Transform title = GetShopItemTitle(i);

            if (title != null)
            {
                shopItemTitleHomePos[i] = title.localPosition;
                shopItemTitleHomeScale[i] = title.localScale;
            }
        }

        shopPriceColorsCached = true;
    }

    /// <summary>
    /// Ценники и доступность покупок в магазине (золото / полное HP или энергия / купленные пассивки).
    /// </summary>
    private void UpdateShopPrices()
    {
        if (!shopPriceColorsCached)
            PrepareShopPrices();

        // Ценники зон HP / MP.
        ApplyShopPriceText(ResolveShopPriceText(shopHpPriceText, shopHpZone), shopHpPrice, shopHpBasePriceColor);
        ApplyShopPriceText(ResolveShopPriceText(shopEnergyPriceText, shopEnergyZone), shopEnergyPrice, shopEnergyBasePriceColor);

        SetShopZoneDimmed(shopHpZone, !IsShopHpAvailable);
        SetShopZoneDimmed(shopEnergyZone, !IsShopEnergyAvailable);

        EnsureShopItems();

        for (int i = 0; i < ShopItemCount; i++)
        {
            Transform zone = GetShopItemZone(i);
            TMP_Text priceText = ResolveShopPriceText(GetShopItemPriceText(i), zone);
            bool sold = IsShopItemSold(i);

            // Купленный релик — Tittle (название и описание) улетает к Info_relics и гаснет.
            if (!sold)
                SetShopItemTitleActive(i, true);

            // Название с текущим уровнем: "ГЕНЕРАТОР (2)".
            ApplyShopRelicName(i);

            // Описание с числами, умноженными на уровень из скобок.
            ApplyShopRelicDescription(i);

            // Проданный релик: вместо цены — SOLD, зона погашена и клик не ловит.
            if (sold)
            {
                ApplyShopSoldText(priceText);
                SetShopZoneDimmed(zone, true);
                continue;
            }

            Color baseColor = shopItemBasePriceColors != null && i < shopItemBasePriceColors.Length
                ? shopItemBasePriceColors[i]
                : Color.white;

            int price = GetShopItemPrice(i);

            ApplyShopPriceText(priceText, price, baseColor);
            SetShopZoneDimmed(zone, price > gold);
        }
    }

    /// <summary>
    /// Ценник зоны: явная ссылка, а если её нет — первый дочерний объект "Cell" внутри зоны.
    /// </summary>
    private static TMP_Text ResolveShopPriceText(TMP_Text explicitText, Transform zone)
    {
        if (explicitText != null)
            return explicitText;

        if (zone == null)
            return null;

        TMP_Text[] texts = zone.GetComponentsInChildren<TMP_Text>(true);

        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null && texts[i].name.StartsWith("Cell"))
                return texts[i];
        }

        return null;
    }

    private Transform GetShopItemZone(int index)
    {
        return shopItemZones != null && index >= 0 && index < shopItemZones.Length
            ? shopItemZones[index]
            : null;
    }

    private TMP_Text GetShopItemPriceText(int index)
    {
        return shopItemPriceTexts != null && index >= 0 && index < shopItemPriceTexts.Length
            ? shopItemPriceTexts[index]
            : null;
    }

    private void ApplyShopPriceText(TMP_Text text, int price, Color baseColor)
    {
        if (text == null)
            return;

        text.text = price.ToString();
        text.color = price > gold ? UpgradeUnaffordablePriceColor : baseColor;
        Translate.ApplyFont(text);
    }

    private static void SetShopZoneDimmed(Transform zone, bool dimmed)
    {
        if (zone == null)
            return;

        ZoneFade fade = zone.GetComponent<ZoneFade>();

        if (fade != null)
            fade.SetDimmed(dimmed);
    }

    private bool IsShopItemZone(Transform zone)
    {
        if (zone == null || shopItemZones == null)
            return false;

        for (int i = 0; i < shopItemZones.Length; i++)
        {
            if (shopItemZones[i] == zone)
                return true;
        }

        return false;
    }

    /// <summary>Релик в этой зоне уже куплен.</summary>
    private bool IsShopItemSold(int index)
    {
        return shopItemSold != null && index >= 0 && index < shopItemSold.Length && shopItemSold[index];
    }

    /// <summary>Можно ли купить релик: ещё не продан и хватает золота.</summary>
    private bool IsShopItemAvailable(int index)
    {
        return index >= 0 && index < ShopItemCount && !IsShopItemSold(index) && gold >= GetShopItemPrice(index);
    }

    /// <summary>
    /// Пересоздаёт витрину: в каждую зону выпадает случайный релик (могут повторяться).
    /// Вызывается на старте забега и после победы над группой врагов.
    /// </summary>
    private void RerollShopRelics()
    {
        EnsureShopItems();

        // Новая витрина — все релики снова по базовой цене.
        int basePrice = Mathf.Max(1, shopItemPrice);

        for (int i = 0; i < ShopItemCount; i++)
            shopItemCurrentPrices[i] = basePrice;

        if (relics == null || relics.Length == 0)
            return;

        for (int i = 0; i < ShopItemCount; i++)
        {
            int relicIndex = Random.Range(0, relics.Length);

            shopItemSold[i] = false;
            shopItemRelicIndex[i] = relicIndex;

            // Новая витрина — Tittle возвращает исходный размер и позицию.
            ResetShopItemTitle(i);
        }

        // Название и описание (с числами под уровень) пишет UpdateShopPrices.
        UpdateShopPrices();
    }

    /// <summary>
    /// Название релика с текущим уровнем в скобках: не куплен — «(1)», куплен один раз — «(2)».
    /// </summary>
    private void ApplyShopRelicName(int index)
    {
        RelicInfo relic = GetShopItemRelic(index);
        TMP_Text text = GetShopItemNameText(index);

        if (relic == null || text == null)
            return;

        int level = Mathf.Max(0, relic.level) + 1;

        text.text = string.Format("{0} ({1})", LocalizeShopKey(relic.nameKey), level);
        Translate.ApplyFont(text);
    }

    /// <summary>
    /// Описание релика: все числа из локализации умножаются на уровень из скобок (1 — без изменений).
    /// </summary>
    private void ApplyShopRelicDescription(int index)
    {
        RelicInfo relic = GetShopItemRelic(index);
        TMP_Text text = GetShopItemDescText(index);

        if (relic == null || text == null)
            return;

        int level = Mathf.Max(0, relic.level) + 1;
        string description = LocalizeShopKey(ResolveRelicDescKey(relic));

        text.text = ScaleRelicNumbers(description, level);
        Translate.ApplyFont(text);
    }

    /// <summary>
    /// Умножает все числа в строке на multiplier, сохраняя знак и разделитель дроби
    /// («0,1» → «0,2», «+5%» → «+10%»). При multiplier = 1 строка не меняется.
    /// </summary>
    private static string ScaleRelicNumbers(string text, int multiplier)
    {
        if (multiplier <= 1 || string.IsNullOrEmpty(text))
            return text;

        return RelicNumberRegex.Replace(text, match =>
        {
            string token = match.Value;
            bool hasFraction = token.IndexOf(',') >= 0 || token.IndexOf('.') >= 0;
            char separator = token.IndexOf(',') >= 0 ? ',' : '.';

            if (!double.TryParse(token.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                return token;

            double scaled = value * multiplier;

            string result = hasFraction
                ? scaled.ToString("0.##########", CultureInfo.InvariantCulture)
                : Mathf.RoundToInt((float)scaled).ToString(CultureInfo.InvariantCulture);

            return separator == ',' ? result.Replace('.', ',') : result;
        });
    }

    /// <summary>Показать/спрятать Tittle зоны релика (название + описание).</summary>
    private void SetShopItemTitleActive(int index, bool active)
    {
        Transform title = GetShopItemTitle(index);

        if (title == null || title.gameObject.activeSelf == active)
            return;

        title.gameObject.SetActive(active);
    }

    private Transform GetShopItemTitle(int index)
    {
        return shopItemTitles != null && index >= 0 && index < shopItemTitles.Length
            ? shopItemTitles[index]
            : null;
    }

    /// <summary>
    /// Купленный релик: Tittle летит к Info_relics, уменьшаясь до нуля, и выключается в конце.
    /// </summary>
    private void FlyRelicTitleToInfo(int index)
    {
        Transform title = GetShopItemTitle(index);

        if (title == null)
            return;

        Transform target = infoRelics != null
            ? infoRelics.transform
            : (infoRelicsText != null ? infoRelicsText.transform : null);

        title.DOKill();

        if (target == null || relicTitleFlyDuration <= 0f)
        {
            title.gameObject.SetActive(false);
            return;
        }

        title.DOScale(Vector3.one * 0.1f, relicTitleFlyDuration)
            .SetEase(Ease.InQuad)
            .SetUpdate(true);

        title.DOMove(target.position, relicTitleFlyDuration)
            .SetEase(Ease.InQuad)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                if (title != null)
                    title.gameObject.SetActive(false);
            });
    }

    /// <summary>
    /// Возвращает Tittle к исходным позиции и масштабу (новая витрина после победы).
    /// </summary>
    private void ResetShopItemTitle(int index)
    {
        Transform title = GetShopItemTitle(index);

        if (title == null)
            return;

        title.DOKill();

        if (shopItemTitleHomePos != null && index >= 0 && index < shopItemTitleHomePos.Length)
            title.localPosition = shopItemTitleHomePos[index];

        if (shopItemTitleHomeScale != null && index >= 0 && index < shopItemTitleHomeScale.Length)
            title.localScale = shopItemTitleHomeScale[index];

        if (!title.gameObject.activeSelf)
            title.gameObject.SetActive(true);
    }

    private bool TryShopBuyItem(int index)
    {
        if (index < 0 || index >= ShopItemCount || IsShopItemSold(index))
            return false;

        int price = GetShopItemPrice(index);

        if (gold < price)
            return false;

        gold -= price;
        shopItemSold[index] = true;

        // Релики повторяются: уровень — сколько раз этот релик уже куплен.
        RelicInfo relic = GetShopItemRelic(index);

        if (relic != null)
        {
            relic.level++;
            SaveRelicLevel(relic);
            ApplyRelicInstantEffect(relic);
            infoRelicsTextDirty = true;
        }

        FBPP.SetInt(SaveGold, gold);
        UpdateGoldUI();

        // Остальные релики в витрине дорожают.
        RaiseRemainingRelicPrices(index);

        UpdateShopPrices();

        // Купили релик — сразу показываем Info_relics.
        SetInfoRelicsVisible(true);

        // Tittle купленного релика улетает к Info_relics и гаснет.
        FlyRelicTitleToInfo(index);

        LogLine(string.Format("Куплен релик «{0}» (уровень {1}) за {2} золота",
            LocalizeShopKey(GetShopItemRelicNameKey(index)), relic != null ? relic.level + 1 : 1, price), LogGoldColor);
        return true;
    }

    private string GetShopItemRelicNameKey(int index)
    {
        RelicInfo relic = GetShopItemRelic(index);
        return relic != null ? relic.nameKey : string.Empty;
    }

    private RelicInfo GetShopItemRelic(int index)
    {
        EnsureRelics();

        if (shopItemRelicIndex == null || index < 0 || index >= shopItemRelicIndex.Length)
            return null;

        int relicIndex = shopItemRelicIndex[index];

        return relics != null && relicIndex >= 0 && relicIndex < relics.Length ? relics[relicIndex] : null;
    }

    /// <summary>Ключ описания релика: явный, иначе ключ названия + "Desc".</summary>
    private static string ResolveRelicDescKey(RelicInfo relic)
    {
        if (relic == null)
            return string.Empty;

        return string.IsNullOrEmpty(relic.descKey) ? relic.nameKey + ShopRelicDescSuffix : relic.descKey;
    }

    /// <summary>Ценник купленного релика: вместо цены пишется SOLD.</summary>
    private void ApplyShopSoldText(TMP_Text text)
    {
        if (text == null)
            return;

        text.text = LocalizeShopKey(ShopSoldKey);
        text.color = shopSoldColor;
        Translate.ApplyFont(text);
    }

    private static string LocalizeShopKey(string key)
    {
        if (string.IsNullOrEmpty(key))
            return string.Empty;

        return LocalizationManager.TryLocalize(key, out string value) ? value.Replace("<br>", "\n") : key;
    }

    private TMP_Text GetShopItemNameText(int index)
    {
        EnsureShopItems();

        if (index < 0 || index >= ShopItemCount)
            return null;

        if (shopItemNameTexts[index] == null)
            shopItemNameTexts[index] = FindShopText(GetShopItemZone(index), "Name_Text");

        return shopItemNameTexts[index];
    }

    private TMP_Text GetShopItemDescText(int index)
    {
        EnsureShopItems();

        if (index < 0 || index >= ShopItemCount)
            return null;

        if (shopItemDescTexts[index] == null)
            shopItemDescTexts[index] = FindShopText(GetShopItemZone(index), "Desc");

        return shopItemDescTexts[index];
    }

    private static TMP_Text FindShopText(Transform zone, string namePrefix)
    {
        if (zone == null)
            return null;

        TMP_Text[] texts = zone.GetComponentsInChildren<TMP_Text>(true);

        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null && texts[i].name.StartsWith(namePrefix))
                return texts[i];
        }

        return null;
    }

    /// <summary>Хватает ли золота на восстановление здоровья (окраска/подсветка/клик).</summary>
    private bool IsShopHpAvailable => gold >= shopHpPrice;

    /// <summary>Хватает ли золота на восстановление энергии (окраска/подсветка/клик).</summary>
    private bool IsShopEnergyAvailable => gold >= shopEnergyPrice;

    private bool TryShopRestoreHp()
    {
        if (gold < shopHpPrice)
            return false;

        // Списываем всегда, даже если HP уже полное — это цена за восстановление.
        gold -= shopHpPrice;
        heroHp = maxHeroHp;

        FBPP.SetInt(SaveGold, gold);
        FBPP.SetInt(SaveHeroHp, heroHp);

        UpdateGoldUI();
        UpdateShopPrices();

        // Fly_health летит от зоны HP к Life; экранное HP обновится после прилёта.
        Transform target = GetLifeTarget();

        if (flyHealthPrefab != null && target != null)
        {
            suppressLifeUi = true;

            FlyToTarget(flyHealthPrefab, GetFlyStartPosition(shopHpZone), target, () =>
            {
                suppressLifeUi = false;
                UpdateLifeUI();
            });
        }
        else
        {
            UpdateLifeUI();
        }

        LogLine(string.Format("Здоровье восстановлено за {0} золота", shopHpPrice), LogGoldColor);
        return true;
    }

    private bool TryShopRestoreEnergy()
    {
        if (gold < shopEnergyPrice)
            return false;

        // Списываем всегда, даже если энергия уже полная — это цена за восстановление.
        gold -= shopEnergyPrice;
        energy = CurrentMaxEnergy;

        FBPP.SetInt(SaveGold, gold);
        FBPP.SetFloat(SaveEnergy, energy);

        UpdateGoldUI();
        UpdateShopPrices();

        // Fly_energy летит от зоны MP к Slow-mo; экранная энергия обновится после прилёта.
        Transform target = GetEnergyTarget();

        if (flyEnergyPrefab != null && target != null)
        {
            suppressEnergyUi = true;

            FlyToTarget(flyEnergyPrefab, GetFlyStartPosition(shopEnergyZone), target, () =>
            {
                suppressEnergyUi = false;
                UpdateEnergyUI();
            });
        }
        else
        {
            UpdateEnergyUI();
        }

        LogLine(string.Format("Энергия восстановлена за {0} золота", shopEnergyPrice), LogGoldColor);
        return true;
    }

    /// <summary>Цель полёта Fly_health (Life).</summary>
    private Transform GetLifeTarget()
    {
        if (lifeFill != null)
            return lifeFill.transform;

        return lifeRoot != null ? lifeRoot.transform : null;
    }

    /// <summary>Цель полёта Fly_energy (Slow-mo).</summary>
    private Transform GetEnergyTarget()
    {
        if (energyBar != null)
            return energyBar;

        return energyText != null ? energyText.transform : null;
    }

    /// <summary>Точка старта Fly-объекта — стрелка в текущем положении.</summary>
    private Vector3 GetFlyStartPosition(Transform fallback)
    {
        if (arrowController != null)
            return arrowController.FlySpawnPosition;

        return fallback != null ? fallback.position : Vector3.zero;
    }

    /// <summary>
    /// Спавнит Fly-объект из пула в точке стрелки и ведёт его к UI-цели.
    /// По прилёте объект прячется (возврат в пул) и вызывается onArrive.
    /// </summary>
    private void FlyToTarget(GameObject prefab, Vector3 from, Transform target, System.Action onArrive)
    {
        if (prefab == null || target == null)
        {
            onArrive?.Invoke();
            return;
        }

        Transform parent = ringTransform != null && ringTransform.parent != null
            ? ringTransform.parent
            : target.parent;

        GameObject fly = GameObjectPool.Spawn(prefab, parent);

        if (fly == null)
        {
            onArrive?.Invoke();
            return;
        }

        // Старт — там, где сейчас стоит стрелка.
        fly.transform.position = from;
        fly.transform.localScale = Vector3.one;

        Vector3 end = target.position;

        fly.transform.DOKill();
        fly.transform.DOMove(end, Mathf.Max(0.01f, flyObjectDuration))
            .SetEase(Ease.InOutQuad)
            .SetUpdate(true) // летит независимо от timeScale
            .OnComplete(() =>
            {
                if (fly != null)
                    fly.SetActive(false); // возврат в пул

                onArrive?.Invoke();
            });
    }

    private void ShowFightZoneOnly()
    {
        SwitchZones(ShowFightZoneOnlyNow);
    }

    private void ShowFightZoneOnlyNow()
    {
        activeStartZones.Clear();

        // Экран между боями — это тоже меню, подложка темнеет, фон обесцвечивается.
        SetBackgroundMenu(true);

        // Между боями — зоны "Бой", "Прокачка" и "Магазин".
        HideUpgradeWindow();
        HideSettingsWindows();
        SetZoneActive(playZone, false);
        SetZoneActive(continueZone, false);
        SetZoneActive(mainMenuZone, false);
        SetZoneActive(exitZone, false);
        SetZoneActive(settingsZone, false);
        SetWindowActive(afterFightWindow, true);
        HideShopWindow();
        HidePauseWindow();
        SetWindowActive(endWindow, false);

        AddStartZone(fightZone, StartZoneAction.Fight);
        AddStartZone(updatesZone, StartZoneAction.Updates);
        AddStartZone(shopZone, StartZoneAction.Shop);
    }

    private void ShowMainMenuZone()
    {
        SwitchZones(ShowMainMenuZoneNow);
    }

    private void ShowMainMenuZoneNow()
    {
        activeStartZones.Clear();

        HideUpgradeWindow();
        HideSettingsWindows();
        SetZoneActive(playZone, false);
        SetZoneActive(continueZone, false);
        SetZoneActive(fightZone, false);
        SetZoneActive(exitZone, false);
        SetZoneActive(settingsZone, false);
        SetZoneActive(updatesZone, false);
        SetZoneActive(shopZone, false);
        SetWindowActive(afterFightWindow, false);
        HideShopWindow();

        // Забег окончен — показываем Window_end с зоной MENU.
        SetWindowActive(endWindow, true);

        AddStartZone(mainMenuZone, StartZoneAction.MainMenu);
    }

    private void HideStartZones()
    {
        activeStartZones.Clear();

        HideUpgradeWindow();
        HideSettingsWindows();

        // Зоны гаснут анимацией (ZoneFade.Hide), а сами окна выключаем только после неё —
        // иначе при старте боя зоны исчезают мгновенно вместе с родителем.
        SetZoneActive(playZone, false);
        SetZoneActive(continueZone, false);
        SetZoneActive(fightZone, false);
        SetZoneActive(exitZone, false);
        SetZoneActive(settingsZone, false);
        SetZoneActive(updatesZone, false);
        SetZoneActive(shopZone, false);
        SetZoneActive(mainMenuZone, false);

        CloseWindowAfterZones(afterFightWindow);
        CloseWindowAfterZones(shopWindow);
        CloseWindowAfterZones(endWindow);
    }

    /// <summary>
    /// Гасит зоны текущего окна анимацией и только после этого выключает само окно.
    /// </summary>
    private void HideZonesAndWindow(Transform window, System.Action onHidden)
    {
        float delay = HideActiveZones();

        if (delay <= 0f)
        {
            SetWindowActive(window, false);
            onHidden?.Invoke();
            return;
        }

        DOVirtual.DelayedCall(delay, () =>
        {
            if (this == null)
                return;

            SetWindowActive(window, false);
            onHidden?.Invoke();
        }, true);
    }

    /// <summary>Выключает окно после того, как его зоны успели погаснуть.</summary>
    private void CloseWindowAfterZones(Transform window)
    {
        if (window == null)
            return;

        float delay = Mathf.Max(0f, startZoneFadeDuration);

        if (delay <= 0f)
        {
            SetWindowActive(window, false);
            return;
        }

        DOVirtual.DelayedCall(delay, () =>
        {
            if (this == null)
                return;

            SetWindowActive(window, false);
        }, true);
    }

    private void AddStartZone(Transform zone, StartZoneAction action, int zoneIndex = -1)
    {
        if (zone == null)
            return;

        ZoneFade fade = PrepareZoneFade(zone);

        SetZoneActive(zone, true);

        // Угол берём у самого объекта: сектор зоны всегда смотрит по локальному "верху" зоны,
        // поэтому поворот трансформа — и есть направление сектора.
        float zoneAngle = NormalizeAngle(zone.eulerAngles.z + startZoneAngleOffset);

        float width = fade != null && fade.FillDegrees > 1f ? fade.FillDegrees : startZoneWidth;

        activeStartZones.Add((zoneAngle, Mathf.Max(1f, width), action, fade, zoneIndex));
    }

    /// <summary>
    /// Подсвечивает зону, в которой сейчас стоит стрелка (fill 0.15 -> 0.2, надпись жёлтая).
    /// </summary>
    private void UpdateStartZoneHighlight()
    {
        if (activeStartZones.Count == 0)
            return;

        float arrowAngle = arrowController != null ? arrowController.ArrowAngle : 0f;
        float halfArrowWidth = arrowController != null ? arrowController.ArrowWidth / 2f : 0f;

        foreach (var entry in activeStartZones)
        {
            if (entry.fade == null)
                continue;

            // Недоступную прокачку и текущее разрешение не подсвечиваем.
            bool canHover = IsZoneInteractive(entry);

            float diff = Mathf.Abs(Mathf.DeltaAngle(arrowAngle, entry.angle));
            entry.fade.SetHighlight(canHover && diff <= entry.width / 2f + halfArrowWidth);
        }
    }

    private ZoneFade PrepareZoneFade(Transform zone)
    {
        if (zone == null)
            return null;

        ZoneFade fade = zone.GetComponent<ZoneFade>();

        if (fade == null)
            fade = zone.gameObject.AddComponent<ZoneFade>();

        fade.duration = startZoneFadeDuration;
        fade.hoverFillPercent = startZoneHoverPercent;
        fade.hoverLabelScale = startZoneHoverLabelScale;
        fade.Init();

        return fade;
    }

    /// <summary>
    /// Смена зон: сначала анимацией гаснут текущие, и только потом появляются нужные.
    /// Если гасить нечего — новые показываются сразу.
    /// </summary>
    private void SwitchZones(System.Action showNewZones)
    {
        float delay = HideActiveZones();

        if (delay <= 0f)
        {
            showNewZones?.Invoke();
            return;
        }

        DOVirtual.DelayedCall(delay, () =>
        {
            if (this == null)
                return;

            showNewZones?.Invoke();
        }, true);
    }

    /// <summary>
    /// Гасит все зарегистрированные зоны анимацией и очищает список.
    /// Возвращает длительность анимации скрытия (0 — гасить было нечего).
    /// </summary>
    private float HideActiveZones()
    {
        float delay = 0f;

        for (int i = 0; i < activeStartZones.Count; i++)
        {
            ZoneFade fade = activeStartZones[i].fade;

            if (fade == null)
                continue;

            // Уже скрытые зоны гаснут мгновенно — их анимацию в задержку не берём.
            bool visible = fade.gameObject.activeInHierarchy;

            fade.Hide();

            if (visible)
                delay = Mathf.Max(delay, fade.duration);
        }

        activeStartZones.Clear();

        return delay;
    }

    private static void SetZoneActive(Transform zone, bool active, bool instant = false)
    {
        if (zone == null)
            return;

        ZoneFade fade = zone.GetComponent<ZoneFade>();

        if (fade == null)
        {
            zone.gameObject.SetActive(active);
            return;
        }

        fade.Init();

        if (active)
            fade.Show(instant);
        else
            fade.Hide(instant);
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }

    /// <summary>
    /// Включает/выключает окно (контейнер зон), не трогая анимации зон.
    /// </summary>
    private static void SetWindowActive(Transform window, bool active)
    {
        if (window != null && window.gameObject.activeSelf != active)
            window.gameObject.SetActive(active);
    }

    private static float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle < 0f) angle += 360f;
        return angle;
    }

    // Кешированный id свойства материала BackgroundFull (AllIn1SpriteShader).
    private static readonly int GreyscaleBlendId = Shader.PropertyToID("_GreyscaleBlend");

    // Экранный размер фона (мировые единицы) — чтобы все фоны были одного размера.
    private Vector2 backgroundReferenceSize;

    /// <summary>
    /// Ставит фон раунда: каждые backgroundChangeLevels уровней берётся следующий спрайт по кругу.
    /// Размер подгоняется так, чтобы фон закрывал тот же экран, что и первый.
    /// </summary>
    private void ApplyRoundBackground()
    {
        if (backgroundFull == null || backgrounds == null || backgrounds.Length == 0)
            return;

        int step = Mathf.Max(1, backgroundChangeLevels);
        int index = Mathf.Abs(roundIndex / step) % backgrounds.Length;
        Sprite sprite = backgrounds[index];

        if (sprite == null)
            return;

        Vector3 scale = backgroundFull.transform.localScale;

        if (backgroundReferenceSize == Vector2.zero)
        {
            // Опорный размер берём у спрайта, стоящего в сцене: он задаёт «как должно выглядеть».
            Sprite reference = backgroundFull.sprite != null ? backgroundFull.sprite : sprite;
            backgroundReferenceSize = new Vector2(reference.bounds.size.x * scale.x, reference.bounds.size.y * scale.y);
        }

        backgroundFull.sprite = sprite;

        // Разные фоны могут быть разных размеров — приводим к одному экранному размеру.
        Vector2 native = sprite.bounds.size;

        if (native.x <= 0.0001f || native.y <= 0.0001f)
            return;

        float cover = Mathf.Max(backgroundReferenceSize.x / native.x, backgroundReferenceSize.y / native.y);

        backgroundFull.transform.localScale = new Vector3(
            cover * Mathf.Sign(scale.x == 0f ? 1f : scale.x),
            cover * Mathf.Sign(scale.y == 0f ? 1f : scale.y),
            scale.z);
    }

    /// <summary>
    /// Фон в состоянии «меню»: чёрная подложка темнеет до backgroundBlackMainMenuAlpha,
    /// а BackgroundFull обесцвечивается (_GreyscaleBlend → 1). В бою — наоборот, всё в ноль.
    /// </summary>
    private void SetBackgroundMenu(bool inMenu, bool instant = false)
    {
        SetBackgroundBlackAlpha(inMenu ? backgroundBlackMainMenuAlpha : 0f, instant);
        SetBackgroundGrayscale(inMenu ? 1f : 0f, instant);
    }

    /// <summary>
    /// Плавно меняет альфу чёрной подложки: в бою 0, в главном меню — backgroundBlackMainMenuAlpha.
    /// </summary>
    private void SetBackgroundBlackAlpha(float target, bool instant = false)
    {
        if (backgroundBlack == null)
            return;

        backgroundBlack.DOKill();

        target = Mathf.Clamp01(target);

        if (instant || backgroundBlackFadeDuration <= 0f)
        {
            Color color = backgroundBlack.color;
            color.a = target;
            backgroundBlack.color = color;
            return;
        }

        backgroundBlack.DOFade(target, backgroundBlackFadeDuration).SetUpdate(true);
    }

    /// <summary>
    /// Плавно ведёт _GreyscaleBlend материала BackgroundFull: 1 — фон в меню обесцвечен, 0 — цветной бой.
    /// </summary>
    private void SetBackgroundGrayscale(float target, bool instant = false)
    {
        if (backgroundFull == null)
            return;

        // .material создаёт личный экземпляр материала — общий ассет не трогаем.
        Material material = backgroundFull.material;

        if (material == null || !material.HasProperty(GreyscaleBlendId))
            return;

        material.DOKill();

        target = Mathf.Clamp01(target);

        if (instant || backgroundBlackFadeDuration <= 0f)
        {
            material.SetFloat(GreyscaleBlendId, target);
            return;
        }

        material.DOFloat(target, "_GreyscaleBlend", backgroundBlackFadeDuration).SetUpdate(true);
    }

    private void SetBattleUiVisible(bool visible)
    {
        if (timerRoot != null)
            timerRoot.SetActive(visible);

        if (healthRoot != null)
            healthRoot.SetActive(visible);
    }

    /// <summary>
    /// Игровой HUD: Life и Slow-mo видны в игре и скрыты в главном меню,
    /// а логотип (GameName) наоборот — только в главном меню.
    /// </summary>
    private void SetInGameHud(bool inGame)
    {
        SetActive(lifeRoot, inGame);
        SetActive(slowMoRoot, inGame);
        SetActive(gameName, !inGame);
    }

    /// <summary>
    /// Надпись раунда (RoundLabelTxt) видна только в бою, запущенном через меню.
    /// </summary>
    private void SetRoundLabelVisible(bool visible)
    {
        if (roundText != null && roundText.gameObject.activeSelf != visible)
            roundText.gameObject.SetActive(visible);
    }

    private void PrepareRound(bool withStartZones)
    {
        state = State.Idle;
        timeLeft = GetConfig(roundIndex).roundDuration + RelicRoundTimeBonus;
        roundStarting = false;
        startDelayLeft = 0f;

        // Сбрасываем отложенные обновления UI (если Fly-объект был в полёте).
        suppressLifeUi = false;
        suppressEnergyUi = false;
        suppressGoldUi = false;
        suppressTimerUi = false;
        waitingDamageFlash = false;
        lastTimerTenths = -1;

        // Серия кликов и накопитель кровотечения начинаются заново.
        relicStreakHits = 0;
        relicBleedAccumulator = 0f;
        timerFailInProgress = false;

        // Новый раунд — новые враги (сбрасываем фиксацию состава).
        roundCharacters = null;

        ClearEnemies();
        UpdateEnemyHealthUI();

        zoneManager.StopRound();

        if (withStartZones)
            ShowStartZones();
        else
            ShowFightZoneOnly();

        SetBattleUiVisible(false);
        SetRoundLabelVisible(false);

        UpdateTimerUI();
        UpdateGoldUI();
        UpdateLifeUI();
        UpdateRoundUI();

        // В Idle (ждём FIGHT) стрелка вращается медленнее, чем в бою, и без учёта прокачки.
        arrowController.StartRotation(IdleArrowSpeed);

        LogLine(string.Format("Начало раунда {0}", roundIndex + 1), LogRoundColor);
    }

    private void OnPlayerTap()
    {
        if (arrowController.IsFrozen)
            return;

        // Если зон на кольце нет — клик ничего не делает.
        if (zoneManager.Zones.Count == 0)
            return;

        float arrowAngle = arrowController.ArrowAngle;
        float halfWidth = arrowController.ArrowWidth / 2f;

        // Пробная зона переиспользуется — без аллокации на каждый клик.
        if (arrowProbe == null)
            arrowProbe = new ZoneData(0f, 0f, null, null);

        arrowProbe.startAngle = arrowAngle - halfWidth;
        arrowProbe.width = arrowController.ArrowWidth;

        ZoneData hitZone = null;
        for (int i = 0; i < zoneManager.Zones.Count; i++)
        {
            ZoneData zone = zoneManager.Zones[i];

            if (zoneManager.ZonesOverlap(arrowProbe, zone))
            {
                hitZone = zone;
                break;
            }
        }

        if (hitZone == null)
        {
            // МАГНИТ: шанс, что промах засчитается кликом по ближайшей зоне.
            RelicInfo magnet = FindRelic(RelicMagnetKey);

            if (magnet != null && magnet.level > 0 && RollRelicChance(magnet) && TryGetNearestZone(arrowAngle, out ZoneData nearestZone))
            {
                LogLine("Магнит: промах засчитан по ближайшей зоне", LogRelicColor);
                zoneManager.RemoveZone(nearestZone);
                ApplyZoneEffect(nearestZone);
                return;
            }

            // СНОРОВКА: промах короче держит стрелку.
            relicStreakHits = 0;
            FreezeArrowOnMiss();
            LogLine("Промах", LogMissColor);
            return;
        }

        // Попадание: зона исчезает, срабатывает её эффект.
        zoneManager.RemoveZone(hitZone);
        ApplyZoneEffect(hitZone);
    }

    /// <summary>Ближайшая к углу стрелки зона (для МАГНИТА).</summary>
    private bool TryGetNearestZone(float angle, out ZoneData result)
    {
        result = null;

        if (zoneManager == null || zoneManager.Zones.Count == 0)
            return false;

        float bestDiff = float.MaxValue;

        for (int i = 0; i < zoneManager.Zones.Count; i++)
        {
            ZoneData zone = zoneManager.Zones[i];

            if (zone == null)
                continue;

            float center = zone.startAngle + zone.width * 0.5f;
            float diff = Mathf.Abs(Mathf.DeltaAngle(angle, center));

            if (diff < bestDiff)
            {
                bestDiff = diff;
                result = zone;
            }
        }

        return result != null;
    }

    /// <summary>Заморозка стрелки при промахе с учётом СНОРОВКИ.</summary>
    private void FreezeArrowOnMiss()
    {
        if (arrowController == null)
            return;

        float duration = arrowController.freezeDuration;
        RelicInfo momentum = FindRelic(RelicMomentumKey);

        if (momentum != null && momentum.level > 0)
            duration *= Mathf.Max(0f, 1f - momentum.value * momentum.level / 100f);

        arrowController.Freeze(duration);
    }

    /// <summary>
    /// Эффект зоны по её типу.
    /// </summary>
    private void ApplyZoneEffect(ZoneData zone)
    {
        if (zone == null)
            return;

        // ТИК: +секунды к таймеру за любое попадание; ЦЕПОЧКА копит серию.
        ApplyRelicOnZoneHit();

        switch (zone.type)
        {
            case CombatZoneType.Heal:
            {
                heroHp = Mathf.Min(maxHeroHp, heroHp + healAmount);
                SaveProgress();

                Transform target = GetLifeTarget();

                if (flyHealthPrefab != null && target != null)
                {
                    suppressLifeUi = true;
                    FlyToTarget(flyHealthPrefab, GetFlyStartPosition(zone.gameObject.transform), target, () =>
                    {
                        suppressLifeUi = false;
                        UpdateLifeUI();
                    });
                }
                else
                {
                    UpdateLifeUI();
                }

                LogLine(string.Format("Лечение: +{0} HP", healAmount), LogHealColor);
                break;
            }

            case CombatZoneType.Gold:
            {
                gold += goldZoneAmount;
                SaveProgress();

                Transform target = goldText != null
                    ? goldText.transform
                    : (goldRoot != null ? goldRoot.transform : null);

                if (flyGoldPrefab != null && target != null)
                {
                    suppressGoldUi = true;
                    FlyToTarget(flyGoldPrefab, GetFlyStartPosition(zone.gameObject.transform), target, () =>
                    {
                        suppressGoldUi = false;
                        UpdateGoldUI();
                    });
                }
                else
                {
                    UpdateGoldUI();
                }

                LogLine(string.Format("Получение золота: {0}", goldZoneAmount), LogGoldColor);
                break;
            }

            case CombatZoneType.Energy:
            {
                energy = Mathf.Min(CurrentMaxEnergy, energy + energyZoneAmount);

                Transform target = GetEnergyTarget();

                if (flyEnergyPrefab != null && target != null)
                {
                    suppressEnergyUi = true;
                    FlyToTarget(flyEnergyPrefab, GetFlyStartPosition(zone.gameObject.transform), target, () =>
                    {
                        suppressEnergyUi = false;
                        UpdateEnergyUI();
                    });
                }
                else
                {
                    UpdateEnergyUI();
                }

                LogLine(string.Format("Получение энергии: {0}", energyZoneAmount), LogEnergyColor);
                break;
            }

            case CombatZoneType.Timer:
            {
                timeLeft += timerZoneSeconds;

                Transform target = timerText != null
                    ? timerText.transform
                    : (timerRoot != null ? timerRoot.transform : null);

                if (flyTimerPrefab != null && target != null)
                {
                    suppressTimerUi = true;

                    // Сбрасываем кеш последнего значения, чтобы цифра обновилась после прилёта.
                    lastTimerTenths = -1;

                    FlyToTarget(flyTimerPrefab, GetFlyStartPosition(zone.gameObject.transform), target, () =>
                    {
                        suppressTimerUi = false;
                        UpdateTimerUI();
                    });
                }
                else
                {
                    UpdateTimerUI();
                }

                LogLine(string.Format("Добавка времени: +{0} с", timerZoneSeconds.ToString("0.##")), LogTimerColor);
                break;
            }

            case CombatZoneType.Trap:
                heroHp = Mathf.Max(0, heroHp - trapDamage);
                SaveProgress();
                UpdateLifeUI();
                FlashDamage();
                LogLine(string.Format("Ловушка: -{0} HP", trapDamage), LogHeroDamageColor);

                if (heroHp <= 0)
                    FinishGame(false);
                break;

            default:
                DamageFrontEnemyWithRelics();
                break;
        }
    }

    /// <summary>Эффекты реликов на попадание в зону: ТИК и счётчик ЦЕПОЧКИ.</summary>
    private void ApplyRelicOnZoneHit()
    {
        relicStreakHits++;

        RelicInfo tick = FindRelic(RelicTickKey);

        if (tick == null || tick.level <= 0)
            return;

        float bonus = tick.value * tick.level;
        timeLeft += bonus;
        lastTimerTenths = -1;
        UpdateTimerUI();
        LogLine(string.Format("Тик: +{0:0.##} с", bonus), LogTimerColor);
    }

    /// <summary>
    /// Урон по первому врагу с учётом реликов: ЦЕПОЧКА (серия), ТОЧНЫЙ ГЛАЗ (крит), ЦЕПНАЯ РЕАКЦИЯ.
    /// </summary>
    private void DamageFrontEnemyWithRelics()
    {
        int damage = CurrentDamage;

        // ЦЕПОЧКА: каждый следующий клик даёт value × уровень к урону И к золоту.
        RelicInfo streak = FindRelic(RelicStreakKey);

        if (streak != null && streak.level > 0 && relicStreakHits > 1)
        {
            int bonus = Mathf.RoundToInt(streak.value * streak.level * (relicStreakHits - 1));

            if (bonus > 0)
            {
                damage += bonus;

                gold += bonus;
                SaveProgress();
                UpdateGoldUI();
                LogLine(string.Format("Цепочка: +{0} золота", bonus), LogGoldColor);
            }
        }

        // ТОЧНЫЙ ГЛАЗ: шанс крита, урон умножается на value × уровень (200% — это ×2).
        RelicInfo deadeye = FindRelic(RelicDeadeyeKey);

        if (deadeye != null && deadeye.level > 0 && RollRelicChance(deadeye))
        {
            float multiplier = Mathf.Max(1f, deadeye.value * deadeye.level / 100f);
            damage = Mathf.RoundToInt(damage * multiplier);
            LogLine(string.Format("Крит: урон x{0:0.##}", multiplier), LogDamageColor);
        }

        // ЦЕПНАЯ РЕАКЦИЯ: шанс, что урон получают сразу все враги.
        RelicInfo chain = FindRelic(RelicChainReactionKey);

        if (chain != null && chain.level > 0 && RollRelicChance(chain))
        {
            DamageAllEnemies(damage);
            return;
        }

        DamageFrontEnemy(damage);
    }

    private void DamageFrontEnemy(int damage)
    {
        if (enemies.Count == 0)
            return;

        EnemyView front = enemies[0];
        if (front == null)
        {
            enemies.RemoveAt(0);
            return;
        }

        front.TakeDamage(damage);
        LogLine(string.Format("Нанесение урона: {0}", damage), LogDamageColor);

        if (front.Hp > 0)
        {
            UpdateEnemyHealthUI();
            return;
        }

        ResolveEnemyDeath();
    }

    /// <summary>
    /// Смерть первого врага: золото, релики за убийство, следующий враг или конец раунда.
    /// </summary>
    private void ResolveEnemyDeath()
    {
        if (enemies.Count == 0)
            return;

        EnemyView front = enemies[0];
        RoundConfig config = GetConfig(roundIndex);

        enemies.RemoveAt(0);

        gold += config.gold;
        LogLine(string.Format("Получение золота: {0}", config.gold), LogGoldColor);

        // Релики за убийство (МАРОДЁР добавляет золото — до сохранения).
        ApplyRelicOnEnemyKilled();

        SaveProgress();
        UpdateGoldUI();

        // Смерть: анимация defeat. Бой замирает, следующий враг выйдет в центр после неё.
        PlayEnemyDefeatAfterZoneFade(front);
    }

    /// <summary>
    /// Сначала даёт исчезнуть зоне, по которой тапнули, и только потом ставит бой на паузу
    /// и запускает анимацию смерти врага.
    /// </summary>
    private void PlayEnemyDefeatAfterZoneFade(EnemyView enemy)
    {
        float zoneFade = zoneManager != null ? zoneManager.FadingZoneTimeLeft : 0f;

        if (zoneFade <= 0.001f)
        {
            PlayEnemyDefeat(enemy);
            return;
        }

        DOVirtual.DelayedCall(zoneFade, () => PlayEnemyDefeat(enemy), true);
    }

    /// <summary>
    /// Запускает анимацию смерти. Пока она играет — таймер, стрелка и зоны стоят.
    /// </summary>
    private void PlayEnemyDefeat(EnemyView enemy)
    {
        pendingDefeats++;
        BeginEnemyDeathPause();

        if (enemy == null)
        {
            EndEnemyDefeat();
            return;
        }

        if (!enemy.HasDefeatAnimation)
        {
            FinishEnemyDefeat(enemy);
            return;
        }

        enemy.PlayDefeat(() => FinishEnemyDefeat(enemy));
    }

    /// <summary>
    /// Смерть доиграна: последний враг группы остаётся лежать, остальные растворяются
    /// через FadeAmount. Бой продолжается (и враг идёт в центр) только после конца фейда.
    /// </summary>
    private void FinishEnemyDefeat(EnemyView enemy)
    {
        if (enemy == null)
        {
            EndEnemyDefeat();
            return;
        }

        if (enemies.Count == 0)
        {
            if (!lingeringEnemies.Contains(enemy))
                lingeringEnemies.Add(enemy);

            EndEnemyDefeat();
            return;
        }

        float fade = enemy.FadeDuration;
        enemy.FadeOutAndDestroy();

        if (fade <= 0.001f)
            EndEnemyDefeat();
        else
            DOVirtual.DelayedCall(fade, EndEnemyDefeat, true);
    }

    /// <summary>Пауза боя на время анимации врага: зоны, стрелка и таймер стоят.</summary>
    private void BeginEnemyDeathPause()
    {
        enemyDeathPause = true;

        // Снимаем slow-mo, чтобы defeat проигрывался в обычном темпе.
        slowMoActive = false;
        Time.timeScale = 1f;

        if (zoneManager != null)
            zoneManager.Paused = true;

        if (arrowController != null)
            arrowController.StopRotation();

        SetActive(healthRoot, false);
    }

    /// <summary>Все анимации смертей закончились — продолжаем бой.</summary>
    private void EndEnemyDefeat()
    {
        pendingDefeats = Mathf.Max(0, pendingDefeats - 1);

        if (pendingDefeats > 0)
            return;

        enemyDeathPause = false;

        if (zoneManager != null)
            zoneManager.Paused = false;

        if (state != State.Fighting)
            return;

        if (enemies.Count == 0)
        {
            UpdateEnemyHealthUI();
            CompleteRound();
            return;
        }

        // Боковой враг сдвигается в центр, бой и стрелка продолжаются.
        EnemyView next = enemies[0];

        if (next != null)
            next.MoveTo(GetSlotPosition(0), enemyMoveDuration, Vector3.one * next.BaseScale);

        SetActive(healthRoot, true);
        UpdateEnemyHealthUI();

        if (arrowController != null)
            arrowController.ResumeRotation();
    }

    /// <summary>
    /// ЦЕПНАЯ РЕАКЦИЯ: урон всем врагам сразу. Смерти обрабатываются как обычные.
    /// </summary>
    private void DamageAllEnemies(int damage)
    {
        if (enemies.Count == 0)
            return;

        RoundConfig config = GetConfig(roundIndex);
        bool killedAny = false;

        // Смерти запускаем после обхода: анимация мёртвого не должна менять список на ходу.
        List<EnemyView> dead = new List<EnemyView>();

        // Обходим с конца: список меняется при смертях.
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            if (i >= enemies.Count)
                continue;

            EnemyView enemy = enemies[i];

            if (enemy == null)
            {
                enemies.RemoveAt(i);
                continue;
            }

            enemy.TakeDamage(damage);

            if (enemy.Hp > 0)
                continue;

            enemies.RemoveAt(i);
            killedAny = true;

            gold += config.gold;
            LogLine(string.Format("Получение золота: {0}", config.gold), LogGoldColor);
            ApplyRelicOnEnemyKilled();

            dead.Add(enemy);
        }

        for (int i = 0; i < dead.Count; i++)
            PlayEnemyDefeatAfterZoneFade(dead[i]);

        if (killedAny)
        {
            SaveProgress();
            UpdateGoldUI();
            return;
        }

        UpdateEnemyHealthUI();
    }

    /// <summary>Эффект релика, срабатывающий сразу при покупке (ИСКРА / ПУЛЬС / ПЕСОЧНЫЕ ЧАСЫ).</summary>
    private void ApplyRelicInstantEffect(RelicInfo relic)
    {
        if (relic == null || relic.level <= 0)
            return;

        // Каждая покупка даёт ровно value: 10 покупок ИСКРЫ = 10 × 10 = 100 энергии суммарно.
        float amount = relic.value;

        switch (relic.nameKey)
        {
            case RelicSparkKey:
                // ИСКРА растит максимум энергии и сразу же добавляет энергию.
                ApplyRelicMaxEnergyBonus();
                energy = Mathf.Min(CurrentMaxEnergy, energy + amount);
                SaveProgress();
                UpdateEnergyUI();
                LogLine(string.Format("Искра: +{0:0.##} к максимуму энергии", amount), LogEnergyColor);
                break;

            case RelicPulseKey:
                // ПУЛЬС растит максимум HP и сразу же лечит.
                ApplyRelicMaxHeroHpBonus();
                heroHp = Mathf.Min(maxHeroHp, heroHp + Mathf.RoundToInt(amount));
                SaveProgress();
                UpdateLifeUI();
                LogLine(string.Format("Пульс: +{0} к максимуму HP", Mathf.RoundToInt(amount)), LogHealColor);
                break;

            case RelicHourglassKey:
                // Часы удлиняют таймер каждого следующего раунда (см. RelicRoundTimeBonus).
                LogLine(string.Format("Песочные часы: +{0:0.##} с к таймеру раунда", amount), LogTimerColor);
                break;
        }
    }

    /// <summary>Эффекты реликов, срабатывающие на убийстве врага.</summary>
    private void ApplyRelicOnEnemyKilled()
    {
        // ТРОФЕЙ ВРЕМЕНИ: +секунды к таймеру.
        RelicInfo timeTrophy = FindRelic(RelicTimeTrophyKey);

        if (timeTrophy != null && timeTrophy.level > 0)
        {
            float bonus = timeTrophy.value * timeTrophy.level;
            timeLeft += bonus;
            lastTimerTenths = -1;
            UpdateTimerUI();
            LogLine(string.Format("Трофей времени: +{0:0.##} с", bonus), LogTimerColor);
        }

        // ГЕНЕРАТОР: +энергия.
        RelicInfo generator = FindRelic(RelicGeneratorKey);

        if (generator != null && generator.level > 0)
        {
            float bonus = generator.value * generator.level;
            energy = Mathf.Min(CurrentMaxEnergy, energy + bonus);
            UpdateEnergyUI();
            LogLine(string.Format("Генератор: +{0:0.##} энергии", bonus), LogEnergyColor);
        }

        // МАРОДЁР: +золото за убийство.
        RelicInfo looter = FindRelic(RelicLooterKey);

        if (looter != null && looter.level > 0)
        {
            int bonus = Mathf.RoundToInt(looter.value * looter.level);
            gold += bonus;
            LogLine(string.Format("Мародёр: +{0} золота", bonus), LogGoldColor);
        }

        // АДРЕНАЛИН: шанс восстановить HP.
        RelicInfo adrenaline = FindRelic(RelicAdrenalineKey);

        if (adrenaline != null && adrenaline.level > 0 && RollRelicChance(adrenaline))
        {
            int heal = Mathf.RoundToInt(adrenaline.value * adrenaline.level);
            heroHp = Mathf.Min(maxHeroHp, heroHp + heal);
            SaveProgress();
            UpdateLifeUI();
            LogLine(string.Format("Адреналин: +{0} HP", heal), LogHealColor);
        }
    }

    private void CompleteRound()
    {
        zoneManager.StopRound();

        if (roundIndex >= roundsCount - 1)
        {
            FinishGame(true);
            return;
        }

        roundIndex++;
        SaveProgress();

        // Новая победа — витрина магазина пересоздаётся (новые релики, SOLD снимается).
        RerollShopRelics();

        PrepareRound(false);
    }

    private void OnTimerFailed()
    {
        if (timerFailInProgress)
            return;

        timerFailInProgress = true;

        // ЩИТ ВРЕМЕНИ: шанс, что урон по таймеру не будет нанесён.
        RelicInfo timeShield = FindRelic(RelicTimeShieldKey);
        bool shielded = timeShield != null && timeShield.level > 0 && RollRelicChance(timeShield);

        // Зоны отключаются, стрелка встаёт — как и раньше. Враг НЕ пропадает: он атакует.
        roundStarting = false;
        startDelayLeft = 0f;

        slowMoActive = false;
        Time.timeScale = 1f;

        if (zoneManager != null)
            zoneManager.StopRound();

        if (arrowController != null)
            arrowController.StopRotation();

        SetActive(timerRoot, false);
        SetActive(healthRoot, false);

        EnemyView attacker = enemies.Count > 0 ? enemies[0] : null;

        // Урон и вспышка — в середине анимации Attack; окно проигрыша — после её конца.
        if (attacker != null && attacker.HasAttackAnimation)
        {
            attacker.PlayAttack(() => ApplyTimerFailDamage(shielded), FinishEnemyAttack);
            return;
        }

        ApplyTimerFailDamage(shielded);
        FinishEnemyAttack();
    }

    /// <summary>
    /// Анимация атаки врага закончилась: если герой погиб — теперь показываем проигрыш.
    /// </summary>
    private void FinishEnemyAttack()
    {
        if (!pendingHeroDeath)
            return;

        pendingHeroDeath = false;
        waitingDamageFlash = false;
        timerFailInProgress = false;

        FinishGame(false);
    }

    /// <summary>
    /// Урон по герою за просроченный таймер (вызывается в середине анимации атаки врага).
    /// </summary>
    private void ApplyTimerFailDamage(bool shielded)
    {
        if (shielded)
        {
            LogLine("Щит времени: урон по таймеру отменён", LogRelicColor);

            // Вспышки нет — раунд перезапускаем сразу.
            RestartRoundAfterDamage();
            return;
        }

        heroHp = Mathf.Max(0, heroHp - heroDamageOnTimerFail);
        SaveProgress();
        UpdateLifeUI();
        FlashDamage();
        LogLine(string.Format("Получение урона: {0}HP", heroDamageOnTimerFail), LogHeroDamageColor);

        if (heroHp <= 0)
        {
            // Окно проигрыша — только после конца анимации атаки (FinishEnemyAttack).
            pendingHeroDeath = true;
            waitingDamageFlash = false;
            return;
        }

        // Враг остаётся на месте, пока горит GetDamage image; после вспышки — новый раунд.
        if (damageOverlay == null)
        {
            RestartRoundAfterDamage();
            return;
        }

        waitingDamageFlash = true;
    }

    /// <summary>
    /// Запуск раунда заново после урона по таймеру (когда вспышка урона погасла).
    /// </summary>
    private void RestartRoundAfterDamage()
    {
        waitingDamageFlash = false;
        timerFailInProgress = false;

        RoundConfig config = GetConfig(roundIndex);
        timeLeft = config.roundDuration + RelicRoundTimeBonus;
        UpdateTimerUI();

        SetActive(timerRoot, true);
        SetActive(healthRoot, true);
        SetRoundLabelVisible(true);

        // Новые враги того же состава — после того как старые растворятся.
        ClearEnemiesAndLingering(() =>
        {
            SpawnEnemies(roundIndex);
            UpdateEnemyHealthUI();
        });

        arrowController.StartRotation(config.arrowSpeed);
        zoneManager.BeginRound(config.zones);
    }

    private void FinishGame(bool win)
    {
        state = State.Finished;

        timerFailInProgress = false;
        pendingHeroDeath = false;
        enemyDeathPause = false;
        pendingDefeats = 0;

        MarkRunFinished();

        zoneManager.StopRound();

        // Зоны боя гасит ShowMainMenuZone(): сначала анимацией уходят текущие, потом появляется MENU.
        // Врагов НЕ убираем: на победе остаётся труп с defeat, на проигрыше — атакующий.
        UpdateEnemyHealthUI();

        // Стрелка НЕ останавливается: крутится, пока игрок не попадёт в зону MENU.
        arrowController.StartRotation(IdleArrowSpeed);

        // Скрываем только родителей таймера и жизней врага.
        SetActive(timerRoot, false);
        SetActive(healthRoot, false);
        SetInfoRelicsVisible(false);
        SetRoundLabelVisible(false);

        // Показываем только нужную надпись.
        SetActive(levelWinObject, win);
        SetActive(levelLoseObject, !win);

        // Экран победы/проигрыша — это меню, подложка темнеет, фон обесцвечивается.
        SetBackgroundMenu(true);

        if (win)
        {
            FBPP.SetBool(SaveWin, true);
            SaveProgress();
            Debug.Log("Level Win");
            LogLine("Level Win", LogRoundColor);
        }
        else
        {
            // Проигрыш: сохраняем только золото, остальной прогресс сбрасывается.
            // При следующем запуске игра начнётся с первого раунда и полным HP героя.
            energy = CurrentMaxEnergy;

            // Проигрыш — купленные релики сбрасываются.
            ResetRelicLevels();
            FBPP.SetInt(SaveGold, gold);
            FBPP.SetInt(SaveHeroHp, maxHeroHp);
            FBPP.SetInt(SaveRound, 0);
            FBPP.SetFloat(SaveEnergy, energy);
            FBPP.SetBool(SaveWin, false);

            Debug.Log("Level Lose");
            LogLine("Level Lose", LogHeroDamageColor);
        }

        // Включаем зону главного меню: клик по ней вернёт в главное меню.
        ShowMainMenuZone();
    }

    /// <summary>
    /// Возврат в главное меню после победы/проигрыша: враги убираются,
    /// сбрасывается весь прогресс кроме золота, включаются зоны главного меню.
    /// </summary>
    private void ResetToMainMenu()
    {
        // Если вышли в меню из паузы — снимаем заморозку и закрываем окно паузы.
        paused = false;
        settingsFromPause = false;
        Time.timeScale = 1f;

        if (zoneManager != null)
            zoneManager.Paused = false;

        DOTween.PlayAll();
        HidePauseWindow();

        ClearEnemies();
        ClearLingeringEnemies();
        UpdateEnemyHealthUI();
        zoneManager.StopRound();

        SetZoneActive(mainMenuZone, false);
        SetActive(levelWinObject, false);
        SetActive(levelLoseObject, false);
        SetWindowActive(endWindow, false);

        logLines.Clear();
        if (levelInfoText != null)
            levelInfoText.text = string.Empty;

        // Сохраняем только золото, остальной прогресс сбрасывается.
        heroHp = maxHeroHp;
        energy = CurrentMaxEnergy;
        roundIndex = 0;
        SetActive(goldRoot, false);
        SetInfoRelicsVisible(false);
        FBPP.SetInt(SaveGold, gold);
        FBPP.SetInt(SaveHeroHp, maxHeroHp);
        FBPP.SetInt(SaveRound, 0);
        FBPP.SetBool(SaveWin, false);

        // PrepareRound включает Idle, стартовые зоны главного меню и вращение стрелки.
        PrepareRound(true);
        UpdateGoldUI();
    }

    // ---------------------------------------------------------------- enemies

    private void SpawnEnemies(int index)
    {
        if (enemyPrefab == null || enemyContainer == null)
        {
            Debug.LogError("[GameManager] Не задан префаб врага или контейнер врагов!");
            return;
        }

        RoundConfig config = GetConfig(index);

        // Враги раунда фиксируются при первом спавне: перезапуск уровня после урона
        // по таймеру выставляет те же самые спрайты.
        if (roundCharacters == null || roundCharacters.Count != config.enemyCount)
            roundCharacters = PickCharacters(index, config.enemyCount);

        List<CharacterData> characters = roundCharacters;

        for (int i = 0; i < config.enemyCount; i++)
        {
            GameObject go = Instantiate(enemyPrefab, enemyContainer);
            go.transform.localPosition = GetSlotPosition(i);
            go.transform.localScale = Vector3.one * (i == 0 ? centerEnemyScale : sideEnemyScale);

            EnemyView view = go.GetComponent<EnemyView>();
            if (view == null)
            {
                Destroy(go);
                continue;
            }

            CharacterData character = characters != null && i < characters.Count ? characters[i] : null;

            view.Setup(character, config.enemyHp, centerEnemyScale);
            enemies.Add(view);
        }
    }

    /// <summary>
    /// Враги группы: сначала те, что заданы для раунда в инспекторе,
    /// остаток — случайные из библиотеки без повторов внутри группы.
    /// Если библиотека не найдена — резервные спрайты (по одному кадру Idle).
    /// </summary>
    private List<CharacterData> PickCharacters(int index, int count)
    {
        List<CharacterData> result = new List<CharacterData>(Mathf.Max(0, count));

        EnemyGroupConfig group = GetRoundEnemyGroup(index);

        if (group != null)
        {
            for (int i = 0; i < group.enemies.Length && result.Count < count; i++)
            {
                CharacterData data = FindCharacter(group.enemies[i]);

                if (data == null)
                {
                    Debug.LogWarning(string.Format(
                        "[GameManager] Раунд {0}: враг «{1}» не найден в CharacterLibrary",
                        index + 1,
                        group.enemies[i]));

                    continue;
                }

                if (!result.Contains(data))
                    result.Add(data);
            }
        }

        if (result.Count >= count)
            return result;

        if (characterLibrary != null && characterLibrary.characters != null)
        {
            List<CharacterData> source = new List<CharacterData>();

            for (int i = 0; i < characterLibrary.characters.Length; i++)
            {
                CharacterData data = characterLibrary.characters[i];

                if (data != null && data.HasIdle && !result.Contains(data))
                    source.Add(data);
            }

            // Берём по одному и выкидываем из пула — без одинаковых в группе.
            while (result.Count < count && source.Count > 0)
            {
                int pick = Random.Range(0, source.Count);
                result.Add(source[pick]);
                source.RemoveAt(pick);
            }
        }

        if (result.Count >= count)
            return result;

        // Библиотеки нет — добиваем пустыми записями: враг будет без кадров (спрайт префаба).
        while (result.Count < count)
            result.Add(null);

        return result;
    }

    /// <summary>Группа врагов для раунда из инспектора (null, если список пуст).</summary>
    private EnemyGroupConfig GetRoundEnemyGroup(int index)
    {
        if (roundEnemyGroups == null || index < 0 || index >= roundEnemyGroups.Length)
            return null;

        EnemyGroupConfig group = roundEnemyGroups[index];

        return group != null && group.enemies != null && group.enemies.Length > 0 ? group : null;
    }

    /// <summary>Враг по id из библиотеки (регистр не важен).</summary>
    private CharacterData FindCharacter(string id)
    {
        if (string.IsNullOrEmpty(id) || characterLibrary == null || characterLibrary.characters == null)
            return null;

        string wanted = id.Trim();

        for (int i = 0; i < characterLibrary.characters.Length; i++)
        {
            CharacterData data = characterLibrary.characters[i];

            if (data != null && string.Equals(data.id, wanted, System.StringComparison.OrdinalIgnoreCase))
                return data;
        }

        return null;
    }

    /// <summary>
    /// Локальная позиция слота внутри enemyContainer: берётся из точек сцены,
    /// если они заданы, иначе — старое смещение от центра кольца.
    /// </summary>
    private Vector3 GetSlotPosition(int slotIndex)
    {
        Vector3 world = GetSlotWorldPosition(slotIndex);

        return enemyContainer != null ? enemyContainer.InverseTransformPoint(world) : world;
    }

    private Vector3 GetSlotWorldPosition(int slotIndex)
    {
        if (enemyPositions != null && enemyPositions.Length > 0)
        {
            int index = Mathf.Clamp(slotIndex, 0, enemyPositions.Length - 1);
            Transform point = enemyPositions[index];

            if (point != null)
                return point.position;
        }

        Vector3 origin = enemyContainer != null ? enemyContainer.position : Vector3.zero;

        switch (slotIndex)
        {
            case 1:
                return origin + new Vector3(-sideOffsetWorld, 0f, 0f);
            case 2:
                return origin + new Vector3(sideOffsetWorld, 0f, 0f);
            default:
                return origin;
        }
    }

    private void ClearEnemies()
    {
        pendingDefeats = 0;
        enemyDeathPause = false;

        foreach (EnemyView enemy in enemies)
        {
            if (enemy != null)
                enemy.FadeOutAndDestroy();
        }

        enemies.Clear();
    }

    /// <summary>
    /// Растворяет живых врагов и трупы, а onComplete вызывает только после конца FadeAmount —
    /// новые враги должны появляться, когда старые уже исчезли.
    /// </summary>
    private void ClearEnemiesAndLingering(System.Action onComplete)
    {
        pendingDefeats = 0;
        enemyDeathPause = false;

        List<EnemyView> fading = new List<EnemyView>();

        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] != null)
                fading.Add(enemies[i]);
        }

        for (int i = 0; i < lingeringEnemies.Count; i++)
        {
            if (lingeringEnemies[i] != null)
                fading.Add(lingeringEnemies[i]);
        }

        enemies.Clear();
        lingeringEnemies.Clear();

        float fade = 0f;

        for (int i = 0; i < fading.Count; i++)
        {
            fade = Mathf.Max(fade, fading[i].FadeDuration);
            fading[i].FadeOutAndDestroy();
        }

        if (fading.Count == 0 || fade <= 0.001f)
        {
            onComplete?.Invoke();
            return;
        }

        DOVirtual.DelayedCall(fade, () => onComplete?.Invoke(), true);
    }

    /// <summary>
    /// Убирает трупы последних врагов. Вызывается при старте нового боя и при уходе в меню.
    /// </summary>
    private void ClearLingeringEnemies()
    {
        foreach (EnemyView enemy in lingeringEnemies)
        {
            if (enemy != null)
                enemy.FadeOutAndDestroy();
        }

        lingeringEnemies.Clear();
    }

    // ---------------------------------------------------------------- slow-mo

    /// <summary>
    /// Зажатая ПКМ во время боя включает слоу-мо и тратит энергию.
    /// </summary>
    private void UpdateSlowMo()
    {
        bool wantSlow = state == State.Fighting
                        && !roundStarting
                        && energy > 0.0001f
                        && Input.GetMouseButton(1);

        if (wantSlow)
        {
            slowMoActive = true;

            // ЭКОНОМ: энергия тратится медленнее.
            float drain = energyDrainPerSecond * RelicEnergyDrainMultiplier;
            energy = Mathf.Max(0f, energy - drain * Time.unscaledDeltaTime);

            if (energy <= 0.0001f)
                slowMoActive = false;
        }
        else
        {
            if (slowMoActive)
            {
                slowMoActive = false;
                SaveProgress();
            }

            if (energyRecoverPerSecond > 0f && energy < CurrentMaxEnergy)
                energy = Mathf.Min(CurrentMaxEnergy, energy + energyRecoverPerSecond * Time.unscaledDeltaTime);
        }

        Time.timeScale = slowMoActive && !HasArrowSpeedRelic ? slowMoTimeScale : 1f;

        // ПОПУТНЫЙ ВЕТЕР / ЯКОРЬ: в слоу-мо время не замедляется, меняется только скорость стрелки.
        if (arrowController != null)
            arrowController.SpeedMultiplier = RelicArrowSpeedMultiplier;

        UpdateEnergyUI();
    }

    /// <summary>ЭКОНОМ: множитель расхода энергии в слоу-мо.</summary>
    private float RelicEnergyDrainMultiplier
    {
        get
        {
            RelicInfo thrift = FindRelic(RelicThriftKey);

            if (thrift == null || thrift.level <= 0)
                return 1f;

            return Mathf.Clamp(1f - thrift.value * thrift.level / 100f, 0.05f, 1f);
        }
    }

    /// <summary>Куплен ли релик, меняющий скорость стрелки вместо замедления времени.</summary>
    private bool HasArrowSpeedRelic => HasRelic(RelicTailwindKey) || HasRelic(RelicAnchorKey);

    /// <summary>ПОПУТНЫЙ ВЕТЕР / ЯКОРЬ: множитель скорости стрелки (работает в слоу-мо).</summary>
    private float RelicArrowSpeedMultiplier
    {
        get
        {
            if (!slowMoActive)
                return 1f;

            float multiplier = 1f;

            RelicInfo tailwind = FindRelic(RelicTailwindKey);

            if (tailwind != null && tailwind.level > 0)
                multiplier += tailwind.value * tailwind.level / 100f;

            RelicInfo anchor = FindRelic(RelicAnchorKey);

            if (anchor != null && anchor.level > 0)
                multiplier -= anchor.value * anchor.level / 100f;

            return Mathf.Max(0.1f, multiplier);
        }
    }

    /// <summary>Релики, работающие каждый кадр боя: РЕЗЕРВУАР (энергия) и КРОВОТЕЧЕНИЕ (урон врагу).</summary>
    private void UpdateRelicBattleEffects(float deltaTime)
    {
        RelicInfo reservoir = FindRelic(RelicReservoirKey);

        if (reservoir != null && reservoir.level > 0 && energy < CurrentMaxEnergy)
        {
            energy = Mathf.Min(CurrentMaxEnergy, energy + reservoir.value * reservoir.level * deltaTime);
            UpdateEnergyUI();
        }

        RelicInfo bleed = FindRelic(RelicBleedKey);

        if (bleed == null || bleed.level <= 0 || enemies.Count == 0)
            return;

        EnemyView front = enemies[0];

        if (front == null || front.Hp <= 0)
            return;

        relicBleedAccumulator += bleed.value * bleed.level * deltaTime;

        int whole = Mathf.FloorToInt(relicBleedAccumulator);

        if (whole <= 0)
            return;

        relicBleedAccumulator -= whole;
        front.TakeDamage(whole);
        LogLine(string.Format("Кровотечение: -{0} HP", whole), LogRelicColor);

        if (front.Hp > 0)
        {
            UpdateEnemyHealthUI();
            return;
        }

        ResolveEnemyDeath();
    }

    /// <summary>
    /// Вспышка урона: альфа GetDamage image доходит до 1 и плавно возвращается к 0.
    /// </summary>
    private void FlashDamage()
    {
        if (damageOverlay == null)
            return;

        damageFlashActive = true;
    }

    private void UpdateDamageOverlay()
    {
        if (damageOverlay == null)
        {
            // Вспышке нечем гореть — отложенный перезапуск раунда делаем сразу.
            if (waitingDamageFlash)
                RestartRoundAfterDamage();

            return;
        }

        if (!damageOverlayCached)
        {
            damageOverlayValue = damageOverlay.color.a;
            damageOverlayCached = true;
        }

        // Пока вспышка активна — идём к 1; достигли — начинаем гаснуть к 0.
        float target = damageFlashActive ? 1f : 0f;

        if (damageFlashActive && damageOverlayValue >= 0.999f)
        {
            damageFlashActive = false;
            target = 0f;
        }

        if (!Mathf.Approximately(damageOverlayValue, target))
        {
            float duration = target > damageOverlayValue ? damageFlashInDuration : damageFlashOutDuration;
            float speed = 1f / Mathf.Max(0.0001f, duration);

            damageOverlayValue = Mathf.MoveTowards(damageOverlayValue, target, speed * Time.unscaledDeltaTime);

            Color color = damageOverlay.color;
            color.a = damageOverlayValue;
            damageOverlay.color = color;
        }

        // Вспышка погасла полностью — теперь можно перезапускать раунд.
        if (waitingDamageFlash && !damageFlashActive && damageOverlayValue <= 0.0001f)
            RestartRoundAfterDamage();
    }

    /// <summary>
    /// Альфа изображения слоу-мо: плавно к slowMoOverlayAlpha, пока слоу-мо активно,
    /// и плавно обратно к 0, когда оно закончилось (кнопку отпустили / энергия кончилась).
    /// </summary>
    private void UpdateSlowMoOverlay()
    {
        if (slowMoOverlay == null)
            return;

        if (!slowMoOverlayCached)
        {
            slowMoOverlayValue = slowMoOverlay.color.a;
            slowMoOverlayCached = true;
        }

        float target = slowMoActive ? Mathf.Clamp01(slowMoOverlayAlpha) : 0f;

        if (Mathf.Approximately(slowMoOverlayValue, target))
            return;

        float speed = 1f / Mathf.Max(0.0001f, slowMoOverlayFadeDuration);
        slowMoOverlayValue = Mathf.MoveTowards(slowMoOverlayValue, target, speed * Time.unscaledDeltaTime);

        Color color = slowMoOverlay.color;
        color.a = slowMoOverlayValue;
        slowMoOverlay.color = color;
    }

    private void UpdateEnergyUI()
    {
        // Пока летит Fly_energy, экранная энергия не обновляется.
        if (suppressEnergyUi)
            return;

        float maxEnergyValue = CurrentMaxEnergy;
        float ratio = maxEnergyValue > 0f ? Mathf.Clamp01(energy / maxEnergyValue) : 0f;

        if (energyBar != null)
        {
            if (energyBaseWidth <= 0f)
                energyBaseWidth = Mathf.Abs(energyBar.sizeDelta.x);

            float width = energyBaseWidth * ratio;

            if (!Mathf.Approximately(width, lastEnergyWidth))
            {
                lastEnergyWidth = width;
                Vector2 size = energyBar.sizeDelta;
                size.x = width;
                energyBar.sizeDelta = size;
            }
        }

        if (energyText != null)
        {
            // Процент считаем от БАЗОВОГО запаса (maxEnergy без бонусов реликов), а не от увеличенного
            // прокачкой/реликами CurrentMaxEnergy — поэтому энергия может показывать > 100%.
            float baseMax = baseMaxEnergy > 0f ? baseMaxEnergy : (maxEnergy > 0f ? maxEnergy : 1f);
            int percent = Mathf.RoundToInt(energy / baseMax * 100f);

            // Текст обновляется только при изменении процента — без строки на каждый кадр.
            if (percent != lastEnergyPercent)
            {
                lastEnergyPercent = percent;
                energyText.text = percent + "%";
            }
        }
    }

    private void OnDisable()
    {
        paused = false;
        pauseTransitionInProgress = false;
        slowMoActive = false;
        Time.timeScale = 1f;

        if (zoneManager != null)
            zoneManager.Paused = false;

        DOTween.PlayAll();
    }

    // ---------------------------------------------------------------- ui

    private void LogLine(string message, Color color)
    {
        if (levelInfoText == null)
            return;

        string hex = ColorUtility.ToHtmlStringRGB(color);
        logLines.Add(string.Format("<color=#{0}>{1}</color>", hex, message));

        int maxLines = GetMaxLogLines();
        while (logLines.Count > maxLines)
            logLines.RemoveAt(0);

        levelInfoText.text = string.Join("\n", logLines);
    }

    private int GetMaxLogLines()
    {
        if (levelInfoText == null)
            return 1;

        float lineHeight = Mathf.Max(1f, levelInfoText.fontSize * 1.2f);
        float height = levelInfoText.rectTransform.rect.height;

        return Mathf.Max(1, Mathf.FloorToInt(height / lineHeight));
    }

    private void UpdateTimerUI()
    {
        // Пока летит Fly_timer, экранный таймер не обновляем.
        if (suppressTimerUi)
            return;

        if (timerText == null)
            return;

        // Строка пересобирается только когда меняется десятая доля секунды (а не каждый кадр).
        int tenths = Mathf.Max(0, Mathf.RoundToInt(timeLeft * 10f));

        if (tenths == lastTimerTenths)
            return;

        lastTimerTenths = tenths;
        timerText.text = (tenths / 10f).ToString("0.0", CultureInfo.InvariantCulture);
    }

    private void UpdateGoldUI()
    {
        // Пока летит Fly_gold, экранное золото не обновляем.
        if (suppressGoldUi)
            return;

        if (goldText != null)
            goldText.text = gold.ToString();
    }

    private void UpdateEnemyHealthUI()
    {
        if (healthText == null)
            return;

        EnemyView front = enemies.Count > 0 ? enemies[0] : null;

        if (front == null)
        {
            healthText.text = string.Empty;
            return;
        }

        healthText.text = LocalizeOr("Localization.HP", "{0} HP", front.Hp);
    }

    private void UpdateLifeUI()
    {
        // Пока летит Fly_health, экранное HP не обновляем.
        if (suppressLifeUi)
            return;

        if (lifeText != null)
            lifeText.text = heroHp + "/" + maxHeroHp;

        if (lifeFill != null)
        {
            float ratio = maxHeroHp > 0 ? (float)heroHp / maxHeroHp : 0f;
            Vector3 scale = lifeFill.transform.localScale;
            scale.x = Mathf.Clamp01(ratio);
            lifeFill.transform.localScale = scale;
        }
    }

    private void UpdateRoundUI()
    {
        if (roundText != null)
            roundText.text = LocalizeOr("Localization.Round", "Round {0}/{1}", roundIndex + 1, roundsCount);
    }

    /// <summary>
    /// Локализация с фолбэком: если ключа нет, возвращает английский вариант.
    /// </summary>
    private static string LocalizeOr(string key, string fallback, params object[] args)
    {
        string value = LocalizationManager.Localize(key, args);

        if (string.IsNullOrEmpty(value) || value == key)
            return args != null && args.Length > 0 ? string.Format(fallback, args) : fallback;

        return value;
    }

    // ---------------------------------------------------------------- save

    private void LoadProgress()
    {
        gold = Mathf.Max(0, FBPP.GetInt(SaveGold, 0));
        heroHp = FBPP.GetInt(SaveHeroHp, maxHeroHp);
        roundIndex = Mathf.Clamp(FBPP.GetInt(SaveRound, 0), 0, roundsCount - 1);

        // Был ли начат забег (для зоны CONTINUE).
        runStarted = FBPP.GetBool(SaveRunStarted, false);

        // Купленные уровни прокачки (до ограничения энергии — максимум зависит от них).
        LoadUpgrades();

        // Купленные релики: уровень = сколько раз куплен (переносится между забегами).
        LoadRelicLevels();

        // Громкость музыки и звуков.
        LoadVolumes();
        energy = Mathf.Clamp(FBPP.GetFloat(SaveEnergy, CurrentMaxEnergy), 0f, CurrentMaxEnergy);

        bool won = FBPP.GetBool(SaveWin, false);
        if (won)
        {
            // После победы начинаем заново, золото сохраняется.
            roundIndex = 0;
            heroHp = maxHeroHp;
            FBPP.SetBool(SaveWin, false);
        }

        if (heroHp <= 0)
            heroHp = maxHeroHp;

        SaveProgress();

        // Надписи и сектора слайдеров громкости.
        UpdateVolumeZones();
    }

    private void SaveProgress()
    {
        FBPP.SetInt(SaveGold, gold);
        FBPP.SetInt(SaveHeroHp, heroHp);
        FBPP.SetInt(SaveRound, roundIndex);
        FBPP.SetFloat(SaveEnergy, energy);
    }

    // ---------------------------------------------------------------- difficulty

    private struct RoundConfig
    {
        public int enemyCount;
        public int enemyHp;
        public int gold;
        public float arrowSpeed;
        public float roundDuration;
        public ZoneRoundSettings zones;
    }

    private static readonly int[] RoundEnemies =
    {
        1, 1, 1, 1, 1,
        2, 2, 2, 2, 2, 2, 2, 2, 2,
        3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3
    };

    private static readonly int[] RoundHp =
    {
        30, 32, 35, 37, 40, 42, 44, 47, 49, 52,
        54, 56, 59, 61, 64, 66, 68, 71, 73, 76,
        78, 80, 83, 85, 88, 90, 92, 95, 97, 100
    };

    private static readonly int[] RoundGold =
    {
        10, 15, 20, 25, 30, 35, 40, 45, 50, 55,
        60, 65, 70, 75, 80, 85, 90, 95, 100, 105,
        110, 115, 120, 125, 130, 135, 140, 145, 150, 155
    };

    // Ширина сектора в градусах: 36° (_FillAmount 0.100) -> 3.6° (_FillAmount 0.010).
    private static readonly float[] RoundZoneWidth =
    {
        36f, 34.9f, 33.8f, 32.7f, 31.5f, 30.4f, 29.3f, 28.2f, 27.1f, 25.9f,
        24.8f, 23.7f, 22.6f, 21.5f, 20.4f, 19.2f, 18.1f, 17f, 15.9f, 14.8f,
        13.7f, 12.5f, 11.4f, 10.3f, 9.2f, 8.1f, 7f, 5.8f, 4.7f, 3.6f
    };

    private static readonly int[] RoundZones =
    {
        3, 3, 3, 3, 3, 3, 3, 3, 3, 3,
        4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
        5, 5, 5, 5, 5, 5, 5, 5, 5, 5
    };

    // 1-5 статичные, 6-10 сжатие, 11-15 вращение в одну сторону,
    // 16-20 вращение в случайную из двух сторон, 21-25 сжатие + вращение,
    // 26-30 сжатие + вращение в случайную сторону.
    private static readonly float[] RoundRotation =
    {
        0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f,
        30f, 40f, 48f, 58f, 66f, 76f, 84f, 94f, 102f, 112f,
        120f, 130f, 138f, 148f, 156f, 166f, 174f, 184f, 192f, 200f
    };

    private static readonly float[] RoundShrink =
    {
        0f, 0f, 0f, 0f, 0f, 4f, 4f, 6f, 6f, 8f,
        0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f,
        10f, 10f, 12f, 12f, 12f, 14f, 14f, 14f, 16f, 16f
    };

    private static readonly bool[] RoundRandomRotation =
    {
        false, false, false, false, false, false, false, false, false, false,
        false, false, false, false, false, true, true, true, true, true,
        false, false, false, false, false, true, true, true, true, true
    };

    private static readonly float[] RoundArrowSpeed =
    {
        90f, 99f, 109f, 118f, 127f, 137f, 146f, 155f, 164f, 174f,
        183f, 192f, 202f, 211f, 220f, 230f, 239f, 248f, 258f, 267f,
        276f, 286f, 295f, 304f, 313f, 323f, 332f, 341f, 351f, 360f
    };

    private static readonly float[] RoundSpawnInterval =
    {
        2.5f, 2.45f, 2.4f, 2.35f, 2.3f, 2.25f, 2.2f, 2.15f, 2.1f, 2.05f,
        2f, 1.95f, 1.9f, 1.85f, 1.8f, 1.75f, 1.7f, 1.65f, 1.6f, 1.55f,
        1.5f, 1.45f, 1.4f, 1.35f, 1.3f, 1.25f, 1.2f, 1.15f, 1.1f, 1f
    };

    /// <summary>
    /// Заполняет кривую сложности значениями по умолчанию, если она пуста или не совпадает по длине.
    /// </summary>
    private void EnsureBalanceCurve()
    {
        if (roundBalance == null || roundBalance.Length != roundsCount)
        {
            roundBalance = new RoundBalance[roundsCount];

            for (int i = 0; i < roundsCount; i++)
                roundBalance[i] = CreateDefaultBalance(i);

            return;
        }

        for (int i = 0; i < roundBalance.Length; i++)
        {
            if (roundBalance[i] == null)
                roundBalance[i] = CreateDefaultBalance(i);
        }
    }

    private RoundBalance CreateDefaultBalance(int index)
    {
        int i = Mathf.Clamp(index, 0, RoundEnemies.Length - 1);

        return new RoundBalance
        {
            enemyCount = RoundEnemies[i],
            enemyHp = RoundHp[i],
            gold = RoundGold[i],
            zoneWidth = RoundZoneWidth[i],
            zoneCount = RoundZones[i],
            rotationSpeed = RoundRotation[i],
            shrinkSpeed = RoundShrink[i],
            randomRotationDirection = RoundRandomRotation[i],
            arrowSpeed = RoundArrowSpeed[i],
            spawnInterval = RoundSpawnInterval[i],
            roundDuration = roundDuration
        };
    }

    private RoundConfig GetConfig(int index)
    {
        EnsureBalanceCurve();

        int i = Mathf.Clamp(index, 0, roundBalance.Length - 1);
        RoundBalance b = roundBalance[i] ?? CreateDefaultBalance(i);

        int zoneCount = Mathf.Max(1, b.zoneCount);

        // Прокачка: шире зоны, медленнее движение/сжатие зон, медленнее стрелка, длиннее таймер.
        UpgradeInfo sizeUpgrade = GetUpgrade(UpgradeZoneSizeIndex);
        UpgradeInfo moveUpgrade = GetUpgrade(UpgradeMoveSpeedIndex);
        UpgradeInfo scaleUpgrade = GetUpgrade(UpgradeScaleSpeedIndex);
        UpgradeInfo arrowUpgrade = GetUpgrade(UpgradeArrowSpeedIndex);
        UpgradeInfo timerUpgrade = GetUpgrade(UpgradeTimerIndex);

        float baseWidth = Mathf.Max(0.1f, b.zoneWidth);
        float width = sizeUpgrade != null ? sizeUpgrade.Apply(baseWidth, 0.1f) : baseWidth;

        float rotation = moveUpgrade != null ? moveUpgrade.Apply(Mathf.Max(0f, b.rotationSpeed), 0f) : b.rotationSpeed;
        float shrink = scaleUpgrade != null ? scaleUpgrade.Apply(Mathf.Max(0f, b.shrinkSpeed), 0f) : b.shrinkSpeed;

        float baseArrow = Mathf.Max(1f, b.arrowSpeed);
        float arrow = arrowUpgrade != null ? arrowUpgrade.Apply(baseArrow, 1f) : baseArrow;

        float baseDuration = b.roundDuration > 0f ? b.roundDuration : roundDuration;
        float duration = timerUpgrade != null ? timerUpgrade.Apply(baseDuration, 1f) : baseDuration;

        // Таймер — с точностью до десятых.
        duration = Mathf.Max(0.1f, Mathf.Round(duration * 10f) / 10f);

        // Состав группы из инспектора задаёт и количество врагов в раунде.
        int enemyCount = Mathf.Max(1, b.enemyCount);
        EnemyGroupConfig group = GetRoundEnemyGroup(i);

        if (group != null)
        {
            int slots = enemyPositions != null && enemyPositions.Length > 0 ? enemyPositions.Length : 3;
            enemyCount = Mathf.Clamp(group.enemies.Length, 1, Mathf.Max(1, slots));
        }

        return new RoundConfig
        {
            enemyCount = enemyCount,
            enemyHp = Mathf.Max(1, b.enemyHp),
            gold = Mathf.Max(0, b.gold),
            arrowSpeed = Mathf.Max(1f, arrow),
            roundDuration = duration,
            zones = new ZoneRoundSettings
            {
                initialZoneCount = Mathf.Min(2, zoneCount),
                maxZoneCount = zoneCount,
                spawnInterval = Mathf.Max(0.05f, b.spawnInterval),
                minZoneWidth = width,
                maxZoneWidth = width,
                minRotationSpeed = rotation,
                maxRotationSpeed = rotation,
                minShrinkSpeed = shrink,
                maxShrinkSpeed = shrink,
                randomRotationDirection = b.randomRotationDirection,
                roundLevel = i + 1
            }
        };
    }

    // ---------------------------------------------------------------- upgrades

    private static readonly string[] UpgradeNames =
    {
        "Размер зон",
        "Урон",
        "Таймер",
        "Энергия",
        "Скорость движения зоны",
        "Скорость масштаба зоны",
        "Скорость стрелки"
    };

    private static readonly string[] UpgradeLocalizationKeys =
    {
        "Localization.UpgradeZoneSize",
        "Localization.UpgradeDamage",
        "Localization.UpgradeTimer",
        "Localization.UpgradeEnergy",
        "Localization.UpgradeMoveSpeed",
        "Localization.UpgradeScaleSpeed",
        "Localization.UpgradeArrowSpeed"
    };

    // Цены по умолчанию: prices[i] — стоимость перехода на уровень (i+1).
    private static readonly int[] UpgradeDefaultPrices = { 20, 45, 80, 130, 200, 290, 400, 540, 720, 950 };

    // Фиксированная прибавка за уровень (минус — параметр уменьшается).
    // Ширина зоны: +2° за уровень; урон: +1 за уровень; таймер: +1 с за уровень;
    // энергия: +10 за уровень; скорость зон: -2°/с за уровень;
    // скорость сжатия: -0.5 за уровень; скорость стрелки: -5°/с за уровень.
    private static readonly float[] UpgradeDefaultEffects = { 2f, 1f, 1f, 10f, -2f, -0.5f, -5f };

    // Индексы строк в upgrades[] (совпадают с UpgradeNames).
    private const int UpgradeZoneSizeIndex = 0;
    private const int UpgradeDamageIndex = 1;
    private const int UpgradeTimerIndex = 2;
    private const int UpgradeEnergyIndex = 3;
    private const int UpgradeMoveSpeedIndex = 4;
    private const int UpgradeScaleSpeedIndex = 5;
    private const int UpgradeArrowSpeedIndex = 6;

    // Текст цены, когда прокачка достигла максимального уровня.
    private const string MaxUpgradePriceLabel = "max";

    // Цвет ценника: максимальный уровень / не хватает золота.
    private static readonly Color UpgradeMaxPriceColor = new Color32(0xFF, 0xE1, 0x00, 0xFF);
    private static readonly Color UpgradeUnaffordablePriceColor = new Color32(0xD9, 0x40, 0x40, 0xFF);

    /// <summary>
    /// Приводит массивы пассивок магазина к размеру ShopItemCount (значения по умолчанию не затирают сцену).
    /// </summary>
    private void EnsureShopItems()
    {
        EnsureRelics();

        shopItemZones = EnsureSize(shopItemZones, ShopItemCount);
        shopItemPriceTexts = EnsureSize(shopItemPriceTexts, ShopItemCount);
        shopItemNameTexts = EnsureSize(shopItemNameTexts, ShopItemCount);
        shopItemDescTexts = EnsureSize(shopItemDescTexts, ShopItemCount);
        shopItemTitles = EnsureSize(shopItemTitles, ShopItemCount);

        if (shopItemSold == null || shopItemSold.Length != ShopItemCount)
            shopItemSold = new bool[ShopItemCount];

        if (shopItemRelicIndex == null || shopItemRelicIndex.Length != ShopItemCount)
            shopItemRelicIndex = new int[ShopItemCount];

        if (shopItemCurrentPrices == null || shopItemCurrentPrices.Length != ShopItemCount)
            shopItemCurrentPrices = new int[ShopItemCount];
    }

    /// <summary>
    /// Если список реликов в инспекторе пуст — подставляем 19 стандартных (ключи и числа из локализации).
    /// </summary>
    private void EnsureRelics()
    {
        if (relics != null && relics.Length > 0)
            return;

        relics = CreateDefaultRelics();
    }

    /// <summary>
    /// Стандартные релики: ключи ClickLocalization.txt и числа из их описаний.
    /// Order: value — основное число, chance — шанс в процентах (0, если шанса нет).
    /// </summary>
    private static RelicInfo[] CreateDefaultRelics()
    {
        return new[]
        {
            // +0,1 сек к таймеру при попадании в зону
            new RelicInfo { nameKey = "Localization.RelicTick", descKey = "Localization.RelicTickDesc", value = 0.1f },
            // +10 энергии
            new RelicInfo { nameKey = "Localization.RelicSpark", descKey = "Localization.RelicSparkDesc", value = 10f },
            // +1 HP
            new RelicInfo { nameKey = "Localization.RelicPulse", descKey = "Localization.RelicPulseDesc", value = 1f },
            // +3 сек таймера
            new RelicInfo { nameKey = "Localization.RelicHourglass", descKey = "Localization.RelicHourglassDesc", value = 3f },
            // энергия тратится медленнее на 10%
            new RelicInfo { nameKey = "Localization.RelicThrift", descKey = "Localization.RelicThriftDesc", value = 10f },
            // +1 сек таймера после каждого убийства
            new RelicInfo { nameKey = "Localization.RelicTimeTrophy", descKey = "Localization.RelicTimeTrophyDesc", value = 1f },
            // +5 энергии после каждого убийства
            new RelicInfo { nameKey = "Localization.RelicGenerator", descKey = "Localization.RelicGeneratorDesc", value = 5f },
            // шанс 5%, восстановить 1 HP после каждого убийства
            new RelicInfo { nameKey = "Localization.RelicAdrenaline", descKey = "Localization.RelicAdrenalineDesc", value = 1f, chance = 5f },
            // +1 золота с врага
            new RelicInfo { nameKey = "Localization.RelicLooter", descKey = "Localization.RelicLooterDesc", value = 1f },
            // шанс 5%, крит 200%
            new RelicInfo { nameKey = "Localization.RelicDeadeye", descKey = "Localization.RelicDeadeyeDesc", value = 200f, chance = 5f },
            // шанс 5% — урон по таймеру не наносится
            new RelicInfo { nameKey = "Localization.RelicTimeShield", descKey = "Localization.RelicTimeShieldDesc", chance = 5f },
            // шанс 5% — урон сразу всем врагам
            new RelicInfo { nameKey = "Localization.RelicChainReaction", descKey = "Localization.RelicChainReactionDesc", chance = 5f },
            // шанс 5% — промах считается кликом по ближайшей зоне
            new RelicInfo { nameKey = "Localization.RelicMagnet", descKey = "Localization.RelicMagnetDesc", chance = 5f },
            // промах уменьшает паузу стрелки на 10%
            new RelicInfo { nameKey = "Localization.RelicMomentum", descKey = "Localization.RelicMomentumDesc", value = 10f },
            // +0,1 энергии в секунду в бою
            new RelicInfo { nameKey = "Localization.RelicReservoir", descKey = "Localization.RelicReservoirDesc", value = 0.1f },
            // каждый следующий клик даёт +1
            new RelicInfo { nameKey = "Localization.RelicStreak", descKey = "Localization.RelicStreakDesc", value = 1f },
            // враг теряет 1 HP в секунду
            new RelicInfo { nameKey = "Localization.RelicBleed", descKey = "Localization.RelicBleedDesc", value = 1f },
            // в слоу-мо стрелка быстрее на 10%
            new RelicInfo { nameKey = "Localization.RelicTailwind", descKey = "Localization.RelicTailwindDesc", value = 10f },
            // в слоу-мо стрелка медленнее на 10%
            new RelicInfo { nameKey = "Localization.RelicAnchor", descKey = "Localization.RelicAnchorDesc", value = 10f }
        };
    }

    /// <summary>Ключ сохранения уровня релика: Localization.RelicTick -> relicLevel_RelicTick.</summary>
    private static string GetRelicLevelSaveKey(RelicInfo relic)
    {
        string id = relic != null ? relic.nameKey : null;

        if (string.IsNullOrEmpty(id))
            return null;

        const string localizationPrefix = "Localization.";

        if (id.StartsWith(localizationPrefix))
            id = id.Substring(localizationPrefix.Length);

        return SaveRelicLevelPrefix + id;
    }

    /// <summary>Прибавка к максимуму энергии от ИСКРЫ (value × уровень).</summary>
    private float RelicMaxEnergyBonus
    {
        get
        {
            RelicInfo spark = FindRelic(RelicSparkKey);
            return spark != null ? spark.value * Mathf.Max(0, spark.level) : 0f;
        }
    }

    /// <summary>Пересчитывает максимум энергии: база из инспектора + бонус ИСКРЫ.</summary>
    private void ApplyRelicMaxEnergyBonus()
    {
        if (baseMaxEnergy <= 0f)
            baseMaxEnergy = Mathf.Max(1f, maxEnergy);

        maxEnergy = Mathf.Max(1f, baseMaxEnergy + RelicMaxEnergyBonus);
        energy = Mathf.Min(energy, CurrentMaxEnergy);
    }

    /// <summary>Прибавка к максимуму HP от ПУЛЬСА (value × уровень).</summary>
    private int RelicMaxHeroHpBonus
    {
        get
        {
            RelicInfo pulse = FindRelic(RelicPulseKey);
            return pulse != null ? Mathf.RoundToInt(pulse.value * Mathf.Max(0, pulse.level)) : 0;
        }
    }

    /// <summary>Пересчитывает максимум HP: база из инспектора + бонус ПУЛЬСА.</summary>
    private void ApplyRelicMaxHeroHpBonus()
    {
        if (baseMaxHeroHp <= 0)
            baseMaxHeroHp = Mathf.Max(1, maxHeroHp);

        maxHeroHp = Mathf.Max(1, baseMaxHeroHp + RelicMaxHeroHpBonus);
        heroHp = Mathf.Min(heroHp, maxHeroHp);
    }

    /// <summary>Бонус к длительности раунда от ПЕСОЧНЫХ ЧАСОВ.</summary>
    private float RelicRoundTimeBonus
    {
        get
        {
            RelicInfo hourglass = FindRelic(RelicHourglassKey);
            return hourglass != null ? hourglass.value * Mathf.Max(0, hourglass.level) : 0f;
        }
    }

    /// <summary>Количество покупок релика по ключу названия (0 — не куплен).</summary>
    private int GetRelicLevel(string nameKey)
    {
        RelicInfo relic = FindRelic(nameKey);
        return relic != null ? Mathf.Max(0, relic.level) : 0;
    }

    /// <summary>Релик по ключу названия (или null).</summary>
    private RelicInfo FindRelic(string nameKey)
    {
        EnsureRelics();

        if (relics == null || string.IsNullOrEmpty(nameKey))
            return null;

        for (int i = 0; i < relics.Length; i++)
        {
            RelicInfo relic = relics[i];

            if (relic != null && relic.nameKey == nameKey)
                return relic;
        }

        return null;
    }

    /// <summary>Релик куплен (уровень больше нуля).</summary>
    private bool HasRelic(string nameKey)
    {
        return GetRelicLevel(nameKey) > 0;
    }

    /// <summary>Бросок шанса релика: chance в процентах × уровень (максимум 100%).</summary>
    private static bool RollRelicChance(RelicInfo relic)
    {
        if (relic == null || relic.chance <= 0f)
            return false;

        float chance = Mathf.Clamp(relic.chance * Mathf.Max(0, relic.level), 0f, 100f);
        return Random.value * 100f < chance;
    }

    /// <summary>Суммарное количество покупок реликов (сумма уровней).</summary>
    private int TotalRelicLevels
    {
        get
        {
            EnsureRelics();

            if (relics == null)
                return 0;

            int total = 0;

            for (int i = 0; i < relics.Length; i++)
            {
                if (relics[i] != null)
                    total += Mathf.Max(0, relics[i].level);
            }

            return total;
        }
    }

    /// <summary>
    /// Info_relics показывается только когда куплено больше одного релика.
    /// </summary>
    private void UpdateInfoRelicsVisibility()
    {
        SetInfoRelicsVisible(TotalRelicLevels > 1);
    }

    /// <summary>Показ/скрытие Info_relics; при скрытии чистим текст подсказки.</summary>
    private void SetInfoRelicsVisible(bool visible)
    {
        SetActive(infoRelics, visible);

        if (visible)
            return;

        infoRelicsHovered = false;
        SetActive(infoRelicsBack, false);

        if (infoRelicsText != null)
            infoRelicsText.text = string.Empty;
    }

    /// <summary>
    /// Наведение на Info_relics: пишем список купленных реликов (название + уровень, ниже описание
    /// с числами, умноженными на уровень). Не наведено — текст пустой.
    /// </summary>
    private void UpdateInfoRelicsHover()
    {
        if (infoRelics == null || infoRelicsText == null)
            return;

        bool hovered = infoRelics.activeInHierarchy && IsPointerOver(infoRelics.transform as RectTransform);

        // Задник подсказки показываем только при наведении.
        SetActive(infoRelicsBack, hovered);

        if (hovered)
        {
            if (infoRelicsTextDirty || !infoRelicsHovered)
            {
                infoRelicsText.text = BuildInfoRelicsText();
                Translate.ApplyFont(infoRelicsText);
                infoRelicsTextDirty = false;
            }
        }
        else if (infoRelicsHovered)
        {
            infoRelicsText.text = string.Empty;
        }

        infoRelicsHovered = hovered;
    }

    private static bool IsPointerOver(RectTransform rect)
    {
        if (rect == null)
            return false;

        Canvas canvas = rect.GetComponentInParent<Canvas>();
        Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;

        return RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition, cam);
    }

    /// <summary>
    /// Текст подсказки: для каждого купленного релика — название с уровнем (шрифт 30),
    /// следующей строкой описание с числами × уровень (шрифт 20), между реликами пустая строка.
    /// </summary>
    private string BuildInfoRelicsText()
    {
        EnsureRelics();

        if (relics == null)
            return string.Empty;

        int nameSize = Mathf.RoundToInt(infoRelicsNameFontSize);
        int descSize = Mathf.RoundToInt(infoRelicsDescFontSize);

        System.Text.StringBuilder builder = new System.Text.StringBuilder();

        for (int i = 0; i < relics.Length; i++)
        {
            RelicInfo relic = relics[i];

            if (relic == null || relic.level <= 0)
                continue;

            // В подсказке — текущий уровень (сколько раз куплен), без +1.
            int level = Mathf.Max(1, relic.level);

            if (builder.Length > 0)
                builder.Append("\n\n");

            builder.Append("<size=").Append(nameSize).Append('>')
                .Append(LocalizeShopKey(relic.nameKey)).Append(" (").Append(level).Append(')')
                .Append("</size>\n");

            builder.Append("<size=").Append(descSize).Append('>')
                .Append(ScaleRelicNumbers(LocalizeShopKey(ResolveRelicDescKey(relic)), level))
                .Append("</size>");
        }

        return builder.ToString();
    }

    /// <summary>Загружает уровни реликов из FBPP (CONTINUE, запуск игры).</summary>
    private void LoadRelicLevels()
    {
        EnsureRelics();

        if (relics == null)
            return;

        for (int i = 0; i < relics.Length; i++)
        {
            RelicInfo relic = relics[i];

            if (relic == null)
                continue;

            string key = GetRelicLevelSaveKey(relic);
            relic.level = string.IsNullOrEmpty(key) ? 0 : Mathf.Max(0, FBPP.GetInt(key, 0));
        }

        infoRelicsTextDirty = true;

        // ИСКРА и ПУЛЬС увеличивают максимумы — пересчитываем после загрузки.
        ApplyRelicMaxEnergyBonus();
        ApplyRelicMaxHeroHpBonus();
    }

    private void SaveRelicLevel(RelicInfo relic)
    {
        if (relic == null)
            return;

        string key = GetRelicLevelSaveKey(relic);

        if (string.IsNullOrEmpty(key))
            return;

        FBPP.SetInt(key, Mathf.Max(0, relic.level));
    }

    /// <summary>Сбрасывает все покупки реликов (новая игра).</summary>
    private void ResetRelicLevels()
    {
        EnsureRelics();

        if (relics == null)
            return;

        for (int i = 0; i < relics.Length; i++)
        {
            RelicInfo relic = relics[i];

            if (relic == null)
                continue;

            relic.level = 0;

            string key = GetRelicLevelSaveKey(relic);

            if (!string.IsNullOrEmpty(key))
                FBPP.DeleteInt(key);
        }

        infoRelicsTextDirty = true;

        // Бонусы реликов сброшены — возвращаем базовые максимумы.
        ApplyRelicMaxEnergyBonus();
        ApplyRelicMaxHeroHpBonus();
    }

    private static T[] EnsureSize<T>(T[] array, int size)
    {
        if (array != null && array.Length == size)
            return array;

        T[] result = new T[size];

        if (array != null)
            System.Array.Copy(array, result, Mathf.Min(array.Length, size));

        return result;
    }

    private int GetShopItemPrice(int index)
    {
        EnsureShopItems();

        if (index < 0 || index >= ShopItemCount)
            return 0;

        // Витрина ещё не создавалась — считаем от базовой цены.
        return shopItemCurrentPrices[index] > 0 ? shopItemCurrentPrices[index] : Mathf.Max(1, shopItemPrice);
    }

    /// <summary>
    /// После покупки релика оставшиеся (не проданные) дорожают в shopItemPriceGrowth раз.
    /// </summary>
    private void RaiseRemainingRelicPrices(int boughtIndex)
    {
        float growth = Mathf.Max(1f, shopItemPriceGrowth);

        for (int i = 0; i < ShopItemCount; i++)
        {
            if (i == boughtIndex || IsShopItemSold(i))
                continue;

            int current = shopItemCurrentPrices[i] > 0 ? shopItemCurrentPrices[i] : Mathf.Max(1, shopItemPrice);
            shopItemCurrentPrices[i] = Mathf.RoundToInt(current * growth);
        }
    }

    private void EnsureUpgrades()
    {
        if (upgrades == null || upgrades.Length != UpgradeNames.Length)
        {
            upgrades = new UpgradeInfo[UpgradeNames.Length];

            for (int i = 0; i < upgrades.Length; i++)
                upgrades[i] = CreateDefaultUpgrade(i);

            return;
        }

        for (int i = 0; i < upgrades.Length; i++)
        {
            if (upgrades[i] == null)
            {
                upgrades[i] = CreateDefaultUpgrade(i);
                continue;
            }

            if (upgrades[i].prices == null || upgrades[i].prices.Length != UpgradeInfo.MaxLevel)
                upgrades[i].prices = (int[])UpgradeDefaultPrices.Clone();

            // В сцене поля эффекта могли ещё не сохраниться — подставляем значения по умолчанию.
            if (Mathf.Approximately(upgrades[i].effectPerLevel, 0f))
            {
                int d = Mathf.Clamp(i, 0, UpgradeDefaultEffects.Length - 1);
                upgrades[i].effectPerLevel = UpgradeDefaultEffects[d];
            }
        }
    }

    /// <summary>
    /// Строка прокачки по индексу (или null).
    /// </summary>
    private UpgradeInfo GetUpgrade(int index)
    {
        EnsureUpgrades();

        if (upgrades == null || index < 0 || index >= upgrades.Length)
            return null;

        return upgrades[index];
    }

    /// <summary>Урон за клик с учётом прокачки.</summary>
    private int CurrentDamage
    {
        get
        {
            UpgradeInfo info = GetUpgrade(UpgradeDamageIndex);
            float value = info != null ? info.Apply(damagePerHit, 1f) : damagePerHit;
            return Mathf.Max(1, Mathf.RoundToInt(value));
        }
    }

    /// <summary>Максимум энергии с учётом прокачки (длительность слоу-мо).</summary>
    private float CurrentMaxEnergy
    {
        get
        {
            UpgradeInfo info = GetUpgrade(UpgradeEnergyIndex);
            return Mathf.Max(1f, info != null ? info.Apply(maxEnergy, 1f) : maxEnergy);
        }
    }

    private UpgradeInfo CreateDefaultUpgrade(int index)
    {
        int i = Mathf.Clamp(index, 0, UpgradeNames.Length - 1);

        return new UpgradeInfo
        {
            name = UpgradeNames[i],
            localizationKey = UpgradeLocalizationKeys[i],
            level = 0,
            prices = (int[])UpgradeDefaultPrices.Clone(),
            effectPerLevel = UpgradeDefaultEffects[i]
        };
    }

    private void OnValidate()
    {
        if (roundsCount <= 0) roundsCount = 30;
        roundsCount = Mathf.Clamp(roundsCount, 1, 30);

        EnsureBalanceCurve();
        EnsureUpgrades();
        EnsureShopItems();
    }
}
