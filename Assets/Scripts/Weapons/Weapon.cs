using FishNet.Object;
using UnityEngine;

public class Weapon : NetworkBehaviour
{
    public ConsoleTextSpawn consoleTextSpawn;

    // weapon values
    [Header("Weapon Values")]
    [SerializeField]
    private WeaponType weaponType;
    [SerializeField]
    protected GameObject prefab;
    [SerializeField]
    protected string _name = "Default Weapon";
    public string Name { get { return _name; } }
    [SerializeField]
    protected float _damage = 5f;
    public float Damage { get { return _damage; } }
    [SerializeField]
    protected float _range = 15f;
    public float Range { get { return _range; } }
    [SerializeField]
    protected float _accuracy = 0.2f;
    public float Accuracy { get { return _accuracy; } }
    [SerializeField]
    protected float _fireRate = 1f;
    public float FireRate { get { return _fireRate; } }
    [SerializeField]
    protected float _fireRateTimer = 0f;
    public float FireRateTimer { get { return _fireRateTimer; } }
    [SerializeField]
    protected float _reloadSpeed = 1f;
    public float ReloadSpeed { get { return _reloadSpeed; } }
    public float _reloadSpeedTimer = 0;

    [Header("Weapon Sound")]
    public AudioClip fireSound;
    public AudioClip reloadSound;

    // ammo values
    [Header("Ammo Values")]
    [SerializeField]
    protected int _maxAmmoCapacity = 30;
    public int MaxAmmoCapacity { get { return _maxAmmoCapacity; } }
    [SerializeField]
    protected int _maxMagAmmoCapacity = 5;
    public int MaxMagAmmoCapacity { get { return _maxMagAmmoCapacity; } }
    [SerializeField]
    protected int _startingAmmo = 15;
    public int StartingAmmo { get { return _startingAmmo; } }
    protected int _currentAmmo = 30;
    public int CurrentAmmo { get { return _currentAmmo; } }
    protected int _currentMagAmmo;
    public int CurrentMagAmmo { get { return _currentMagAmmo; } }

    // useful props
    public bool IsEmptyClip { get { return _currentMagAmmo <= 0 && !FireType.Equals(WeaponFireType.Melee); } }
    public bool IsEmptyWeapon { get { return _currentAmmo <= 0 && _currentMagAmmo <= 0 && !FireType.Equals(WeaponFireType.Melee); } }
    protected bool _isReloading = false;
    public bool IsReloading { get { return _isReloading; } }
    [SerializeField]
    protected WeaponFireType _fireType;
    public WeaponFireType FireType { get { return _fireType; } }


    private void Start()
    {
        // ensures starting ammo and max mag ammo capacity are lower than max values
        if (_startingAmmo > _maxAmmoCapacity)
            _startingAmmo = _maxAmmoCapacity;
        if (_maxMagAmmoCapacity > _maxAmmoCapacity)
            _maxMagAmmoCapacity = _maxAmmoCapacity;

        _currentAmmo = _startingAmmo;
        _currentMagAmmo = _maxMagAmmoCapacity;

        consoleTextSpawn = GameObject.FindGameObjectWithTag(StringData.PlayerHUDTag).GetComponent<ConsoleTextSpawn>();

        if (!_fireType.Equals(WeaponFireType.Melee))
            gameObject.SetActive(false);
    }

    /// <summary>
    /// Consists of all the methods which are required to run in Update
    /// </summary>
    /// <param name="player"></param>
    public void WeaponUpdate(PlayerController player)
    {
        FireWeapon(player);
        ReloadHander(player);

        Vector3 origin = player.vCam.transform.position;
        Vector3 spreadDistance = player.vCam.transform.forward + new Vector3(Random.insideUnitCircle.normalized.x * _accuracy, Random.insideUnitCircle.normalized.y * _accuracy, 0f);

        Debug.DrawRay(origin, spreadDistance * _range, Color.red);
    }


    #region Weapon Firing Logic
    /// <summary>
    /// Returns the appropriate firing input depending on WeaponFireType.
    /// </summary>
    /// <param name="player"></param>
    protected virtual void FireWeapon(PlayerController player)
    {
        if (_fireType.Equals(WeaponFireType.Automatic))
        {
            if (Input.GetKey(KeyCode.F))
            {
                FireRateHandler(player);
            }
            else
            {
                _fireRateTimer = 0f;
                player.SetAnimBool(StringData.IsFiring, false);
            }
        }
        else if (_fireType.Equals(WeaponFireType.SemiAutomatic))
        {
            if (Input.GetKeyUp(KeyCode.F))
            {
                FireRateHandler(player);
            }
            else
            {
                _fireRateTimer = 0f;
                player.SetAnimBool(StringData.IsFiring, false);
            }
        }
        else if (_fireType.Equals(WeaponFireType.Burst))
        {
            if (Input.GetKeyUp(KeyCode.F))
            {
                FireRateHandler(player);
            }
            else
            {
                _fireRateTimer = 0f;
                player.SetAnimBool(StringData.IsFiring, false);
            }
        }
        else if (_fireType.Equals(WeaponFireType.Melee))
        {
            if (Input.GetKey(KeyCode.F))
            {
                FireRateHandler(player);
            }
            else
            {
                _fireRateTimer = 0f;
                player.SetAnimBool(StringData.IsFiring, false);
            }
        }
    }

    /// <summary>
    /// Handles the fire rate cooldown of the weapon
    /// </summary>
    /// <param name="cam"></param>
    private void FireRateHandler(PlayerController player)
    {
        // Checks if player has ammo
        if (IsEmptyClip || IsEmptyWeapon)
        {
            player.SetAnimBool(StringData.IsFiring, false);
            Debug.Log("Player needs to reload!");
            consoleTextSpawn.SpawnConsoleText("Player needs to reload!");
            return;
        }

        if (_isReloading)
            CancelReload(player);

        if (_fireRateTimer <= 0)
        {
            player.SetAnimBool(StringData.IsFiring, true);
            player.PlayAnim(StringData.Fire, weaponType);

            Debug.Log("Raycast/Bullet fired!");
            consoleTextSpawn.SpawnConsoleText("Raycast/Bullet fired!");
            ActivateRaycast(player);
            _fireRateTimer = _fireRate;

            // Update ammo ONLY if WeaponFireType is NOT Melee   
            if (!FireType.Equals(WeaponFireType.Melee))
            {
                _currentMagAmmo -= 1;
                player.playerHUD.UpdateAmmo(_currentMagAmmo.ToString(), _currentAmmo.ToString());
            }
        }
        else
        {
            Debug.Log("Raycast on Cooldown");
            consoleTextSpawn.SpawnConsoleText("Raycast on Cooldown");
            _fireRateTimer -= Time.deltaTime;
        }
    }

    /// <summary>
    /// Fires weapon raycast to detect targets
    /// </summary>
    /// <param name="cam"></param>
    private void ActivateRaycast(PlayerController player)
    {
        Vector3 origin = player.vCam.transform.position;
        Vector3 spreadDistance = (player.vCam.transform.forward + new Vector3(Random.insideUnitCircle.normalized.x * _accuracy, Random.insideUnitCircle.normalized.y * _accuracy, 0f)) * _range;
        RaycastHit hit;

        if (Physics.Raycast(origin, spreadDistance, out hit, _range))
        {
            //Debug.DrawRay(origin, spreadDistance, Color.red);
            if (hit.collider.tag.Equals(StringData.PlayerTag))
            {
                PlayerController target = hit.collider.gameObject.GetComponentInParent<PlayerController>();
                Debug.Log("target: " + target.name);
                consoleTextSpawn.SpawnConsoleText("target: " + target.name);
                ApplyDamage(player, target);
            }
        }
    }

    /// <summary>
    /// Applies weapon damage to targets
    /// </summary>
    /// <param name="target"></param>
    [ServerRpc]
    private void ApplyDamage(PlayerController player, PlayerController target)
    {
        if (!target.playerTeam.currentTeam.Equals(player.playerTeam.currentTeam))
        {
            target.currentHealth -= _damage;
            if (target.currentHealth <= 0)
            {
                string log = target.name + " killed by " + player.name;
                GlobalGameData.Instance.AddKillLog(log);
                UpdateKillLog(player, target);
            }
            Debug.Log(-_damage + " points of damage applied to " + target.name);
            consoleTextSpawn.SpawnConsoleText(-_damage + " points of damage applied to " + target.name);
        }
        else
            Debug.Log("Cannot friendly fire!");
        consoleTextSpawn.SpawnConsoleText("Cannot friendly fire!");
    }   

    [ObserversRpc]
    private void UpdateKillLog(PlayerController player, PlayerController target)
    {
        if (target.globalHUD != null)
            target.globalHUD.UpdateGlobalMessagingWindow();
    }

    #endregion

    #region Reloading Logic
    /// <summary>
    /// Initials weapon reloading
    /// </summary>
    private void InitiateReload(PlayerController player)
    {
        if (Input.GetKeyDown(KeyCode.R) && !_isReloading)
        {
            // returns if no ammo
            if (IsEmptyWeapon)
            {
                Debug.Log("You cannot reload since you have no ammo!");
                consoleTextSpawn.SpawnConsoleText("You cannot reload since you have no ammo!");
                return;
            }
            _reloadSpeedTimer = 0;
            _isReloading = true;

            player.SetAnimBool(StringData.IsReloading, true);
            player.PlayAnim(StringData.Reload, weaponType);
            player.playerHUD.UpdateFeedbackText("Reloading...");
        }
    }

    /// <summary>
    /// Handles reload cooldown
    /// </summary>
    protected virtual void ReloadHander(PlayerController player)
    {
        if (_currentMagAmmo < _maxMagAmmoCapacity)
        {
            InitiateReload(player);
        }

        // reload after reload time
        if (_reloadSpeedTimer >= _reloadSpeed)
        {
            Reload(player);
            _reloadSpeedTimer = 0;
            _isReloading = false;
            player.playerHUD.DisableFeedbackText();

            player.SetAnimBool(StringData.IsReloading, false);
        }
        else if (_isReloading)
        {
            Debug.Log("Reloading");
            consoleTextSpawn.SpawnConsoleText("Reloading");
            _reloadSpeedTimer += Time.deltaTime;
        }
    }

    public virtual void CancelReload(PlayerController player)
    {
        _reloadSpeedTimer = 0;
        _isReloading = false;
        player.SetAnimBool(StringData.IsReloading, false);

        player.playerHUD.DisableFeedbackText();
    }

    /// <summary>
    /// Updates the ammo amount in the magizine and total ammo
    /// </summary>
    private void Reload(PlayerController player)
    {
        int ammoNeeded = _maxMagAmmoCapacity - _currentMagAmmo;
        if (ammoNeeded > _currentAmmo)
            ammoNeeded = _currentAmmo;

        _currentMagAmmo += ammoNeeded;

        _currentAmmo -= ammoNeeded;
        if (_currentAmmo <= 0)
            _currentAmmo = 0;

        player.playerHUD.UpdateAmmo(_currentMagAmmo.ToString(), _currentAmmo.ToString());
    }
    #endregion

    /// <summary>
    /// Resets weapon values to its original values
    /// </summary>
    public void ResetWeapon()
    {
        _currentAmmo = _startingAmmo;
        _currentMagAmmo = _maxMagAmmoCapacity;
    }
}
