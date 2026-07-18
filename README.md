# Megastore Product Search for Megastore Simulator

A native search bar for the Products window in **Megastore Simulator**.

Designed to blend seamlessly with the game's interface while making it much easier to find products.

---

## Features

- Native Unity UI search bar
- Live filtering while typing
- Clear button
- Live product count
- Smart search filters
- Supports localized product names
- Lightweight and performance friendly

---

## Search Examples

| Search | Description |
|---------|-------------|
| `milk` | Find products containing "milk" |
| `shelf:cool` | Show products stored on cool shelves |
| `group:bakery` | Show bakery products |
| `department:bakery` | Same as group search |
| `license:3` | Show products requiring License 3 |
| `lic:2` | Short version of license search |

Multiple search terms can also be combined.

Example:

```text
license:3 shelf:cool
```
---

## Installation

### Requirements

- BepInEx

### Install

1. Download `MegastoreProductSearch.dll`
2. Place it inside:

```text
BepInEx/plugins
```

3. Launch the game.

---

## Current Version

**v1.0.0**

---

## Source Code

The complete source code is included in this repository.

---

## Feedback

If you find any bugs or have ideas for future improvements, feel free to open an Issue on GitHub.

---

Created by **RocketKitten**
