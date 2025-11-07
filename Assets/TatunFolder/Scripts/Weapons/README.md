# Descent-Style Weapon System

A modular, easy-to-use weapon system for Descent-style space combat games.

## Features

- **Modular Design**: Easy to add new weapon types by inheriting from `WeaponBase`
- **Multiple Weapon Types**: Includes hitscan (laser) and projectile weapons out of the box
- **Automatic Prefab Instantiation**: Supports both prefab references and scene objects
- **Weapon Switching**: Built-in support for cycling through weapons
- **Input Handling**: Optional integrated input system support
- **Aim Smoothing**: Configurable aim smoothing to stabilize camera sway

## Quick Start

### 1. Adding Weapons to Your Player

1. Add the `WeaponManager` component to your player GameObject
2. Assign the `aimCamera` field to your player's camera
3. Assign the `ownerRb` field to your player's Rigidbody
4. Add weapon prefabs to the `weapons` list in the Inspector

**Note**: You can drag weapon prefabs directly into the list - they will be automatically instantiated at runtime!

### 2. Creating a New Weapon Type

```csharp
using UnityEngine;

public class MyCustomWeapon : WeaponBase
{
    [Header("Custom Settings")]
    public float myParameter = 10f;

    public override void Fire(Ray aimRay)
    {
        if (!CanFire()) return;
        NoteFire();

        // Your weapon logic here
        Debug.Log("Custom weapon fired!");
    }
}
```

### 3. Using the Weapon System

**Fire the current weapon:**
```csharp
weaponManager.FireCurrent();
```

**Switch weapons:**
```csharp
weaponManager.NextWeapon();
weaponManager.PreviousWeapon();
weaponManager.SwitchTo(0); // Switch to first weapon
```

**Add/Remove weapons at runtime:**
```csharp
WeaponBase newWeapon = weaponManager.AddWeapon(myWeaponPrefab);
weaponManager.RemoveWeapon(0);
```

**Get weapon info:**
```csharp
WeaponBase current = weaponManager.GetCurrentWeapon();
int weaponCount = weaponManager.GetWeaponCount();
int currentIndex = weaponManager.GetCurrentWeaponIndex();
```

## Weapon Types

### HitScanWeapon (Laser/Beam Weapons)

Instant-hit weapons using raycasting. Perfect for laser weapons.

**Properties:**
- `range`: Maximum shooting range
- `damage`: Damage per hit
- `impactPrefab`: Visual effect at hit point
- `tracerPrefab`: Beam/tracer visual effect
- `tracerLifetime`: How long the tracer stays visible

### ProjectileWeapon (Rocket/Plasma Weapons)

Physical projectile weapons that spawn moving objects.

**Properties:**
- `projectilePrefab`: Prefab to spawn (must have `Projectile` component)
- `projectileSpeed`: Initial speed of projectile
- `spawnOffset`: Forward offset from muzzle to prevent self-collision

## WeaponBase Properties

All weapons inherit these properties:

- `muzzle`: Transform where projectiles/tracers spawn
- `cooldown`: Minimum time between shots (in seconds)
- `hitMask`: Layer mask for collision detection

## Input Configuration

The WeaponManager can handle input automatically:

1. Set `handleInput` to true
2. Assign your fire action to `fireAction`
3. Configure `fireHoldToAuto` for hold-to-fire or single-press
4. Adjust `fireHoldThreshold` for input sensitivity

**Aim Smoothing:**
- Set `aimSmoothing` to stabilize aim (recommended: 4-12)
- Set to 0 for no smoothing

## Architecture

```
WeaponBase (abstract)
├── HitScanWeapon
├── ProjectileWeapon
└── Your custom weapons

WeaponManager
└── Manages weapon lifecycle and input

IDamageable (interface)
└── Implement this on objects that can take damage
```

## Example Setup

1. Create a weapon prefab with `HitScanWeapon` component
2. Assign a muzzle transform (usually a child empty GameObject)
3. Set damage, range, and cooldown values
4. Optionally assign impact and tracer prefabs
5. Add the weapon prefab to your player's WeaponManager

The weapon system will automatically:
- Instantiate the weapon as a child of the player
- Initialize it with camera and rigidbody references
- Handle equip/unequip and activation
- Manage firing and cooldowns

## Tips

- **Muzzle Position**: Create an empty GameObject as a child of your player at the weapon's visual muzzle point
- **Prefab References**: You can add prefabs directly to the weapons list - no need to manually place them in the scene
- **Layer Masks**: Use `hitMask` to control what each weapon can hit (e.g., exclude player layer)
- **Visual Effects**: Tracer/impact prefabs should clean up automatically (use limited lifetime particles)

## Extending the System

To add a new weapon type:

1. Create a new class inheriting from `WeaponBase`
2. Override the `Fire(Ray aimRay)` method
3. Use `CanFire()` to check cooldown, `NoteFire()` to register firing
4. Access `aimCamera` and `ownerRb` for advanced behavior
5. Optionally override `OnEquip()` and `OnUnequip()` for custom behavior

That's it! The WeaponManager will handle the rest.
