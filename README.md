<div align="center">

# 🎮 TaxVenture

### A Mobile Game Application for Income Tax Literacy Utilizing the Finite State Machine Algorithm

<p align="center">
A 3D scenario-based mobile game designed to improve income tax literacy through interactive gameplay, case-solving mechanics, and Finite State Machine (FSM) driven progression.
</p>

![Unity](https://img.shields.io/badge/Unity-6-black?style=for-the-badge&logo=unity)
![Platform](https://img.shields.io/badge/Platform-Android-green?style=for-the-badge)
![Language](https://img.shields.io/badge/C%23-.NET-blue?style=for-the-badge)
![Status](https://img.shields.io/badge/Status-Development-orange?style=for-the-badge)
![License](https://img.shields.io/badge/License-Educational-blue?style=for-the-badge)

</div>

---

# Table of Contents

- About the Project
- Project Objectives
- Research Background
- Features
- Technologies
- Development Roadmap
- Game Architecture
- Project Structure
- Getting Started
- Development Workflow
- Coding Standards
- FSM Design
- Save System
- Current Progress
- Future Milestones
- Contributors
- License

---

# About the Project

**TaxVenture** is a **3D scenario-based mobile educational game** developed as a supplementary learning tool to improve **income tax literacy**.

Players assume the role of a **Tax Consultant** working inside an office where they perform realistic taxation-related tasks such as:

- Reviewing financial documents
- Identifying taxable and non-taxable income
- Classifying deductions
- Computing taxes
- Preparing tax returns
- Conducting compliance audits
- Solving tax-related cases

Unlike traditional learning materials, TaxVenture transforms taxation into an interactive simulation where players learn by completing realistic office scenarios.

The project uses the **Finite State Machine (FSM)** algorithm to control:

- Overall game progression
- NPC behavior
- Dialogue progression
- Case progression
- Player objectives

---

# Research Background

Tax literacy remains a persistent challenge among Filipino youth, adults, and even accounting students. Traditional instructional methods often struggle to simplify complex concepts such as:

- Income classification
- Taxable vs non-taxable income
- Allowable deductions
- Compliance procedures
- Tax computation

TaxVenture addresses this problem by providing an interactive 3D learning environment where players actively apply taxation concepts through scenario-based gameplay.

The study supports:

- **Sustainable Development Goal 4**
  - Quality Education

- **Sustainable Development Goal 8**
  - Decent Work and Economic Growth

by promoting financial literacy, tax compliance awareness, and informed financial decision-making.

---

# Project Objectives

## General Objective

Develop a **3D Mobile Game Application** entitled:

> **TaxVenture: A Mobile Game Application for Income Tax Literacy Utilizing the Finite State Machine Algorithm**

as a supplementary educational tool that helps individuals build tax literacy and understand income taxation processes.

## Specific Objectives

- Design interactive 3D office environments.

- Create scenario-based taxation gameplay.

- Develop educational case-solving mechanics.

- Apply the Finite State Machine algorithm for:
  - Game progression
  - NPC interaction
  - Objective management
  - Case flow

- Develop a scalable architecture for future expansion.

---

# Features

## Gameplay

- Third-person office exploration

- First-person workstation interaction

- Scenario-based taxation cases

- Educational dialogue

- Case progression

- Document review

- Income classification

- Tax computation

- Compliance auditing

- Case completion rewards

---

## Technical

- Unity 6

- Mobile-first architecture

- Modular codebase

- Event-driven architecture

- Scriptable Objects

- JSON Save System

- Expandable FSM

- Placeholder greybox prototype

---

# Technologies

| Category        | Technology            |
| --------------- | --------------------- |
| Engine          | Unity 6               |
| Language        | C#                    |
| IDE             | Visual Studio / Rider |
| Version Control | Git                   |
| Platform        | Android               |
| Serialization   | JSON                  |
| Architecture    | FSM + Event Driven    |

---

# Development Roadmap

The project is being developed incrementally.

## Phase 1 — Greybox Prototype

Focus:

- Core gameplay loop
- Placeholder assets
- Functional systems
- Scalable architecture

No:

- Final models
- Animations
- Audio
- Visual polish
- Optimization

---

### Planned Milestones

- ✅ Milestone 1
  - Project setup
  - Folder structure
  - Greybox office
  - Third-person controller

- ⏳ Milestone 2
  - Interaction System

- ⏳ Milestone 3
  - Camera Switching

- ⏳ Milestone 4
  - Game FSM

- ⏳ Milestone 5
  - Case Folder

- ⏳ Milestone 6
  - Document Viewer

- ⏳ Milestone 7
  - Dialogue System

- ⏳ Milestone 8
  - NPC FSM

- ⏳ Milestone 9
  - Tax Research

- ⏳ Milestone 10
  - Tax Calculator

- ⏳ Milestone 11
  - Evidence Board

- ⏳ Milestone 12
  - Stamp Assessment

- ⏳ Milestone 13
  - Compliance Audit

- ⏳ Milestone 14
  - Rewards

- ⏳ Milestone 15
  - Case Complete
  - Return to Main Menu

---

# Game Architecture

The project follows a modular architecture designed for long-term scalability.

```
Player
      │
      ▼
Input Layer
      │
      ▼
Interaction System
      │
      ▼
Game FSM
      │
      ▼
Case Systems
      │
      ▼
NPC FSM
      │
      ▼
UI
      │
      ▼
Save System
```

---

# Project Structure

```
Assets/

├── Animations/

├── Audio/

├── Materials/

├── Models/

├── Prefabs/

├── Resources/

├── Scenes/

│   ├── MainMenu
│   ├── OfficePrototype

├── ScriptableObjects/

├── Scripts/

│   ├── Core/
│   │
│   ├── FSM/
│   │   ├── GameStates/
│   │   ├── NPCStates/
│   │
│   ├── Managers/
│   │
│   ├── Player/
│   │
│   ├── Camera/
│   │
│   ├── Interactables/
│   │
│   ├── NPC/
│   │
│   ├── UI/
│   │
│   ├── Save/
│   │
│   ├── Data/
│   │
│   ├── Events/
│   │
│   └── Utilities/

└── Textures/
```

---

# Getting Started

## Requirements

- Unity 6 LTS
- Visual Studio 2022
- Git

---

## Clone Repository

```bash
git clone https://github.com/yourusername/TaxVenture.git
```

---

## Open Project

Open Unity Hub

Select

```
Add Project
```

Choose

```
TaxVenture
```

Open with Unity 6.

---

# Development Workflow

The project is intentionally developed milestone-by-milestone.

Each milestone must satisfy the following:

- Fully playable
- Fully tested
- Modular
- Well documented

before continuing to the next milestone.

---

# Prototype Controls

Current (Editor)

| Action   | Input      |
| -------- | ---------- |
| Move     | WASD       |
| Look     | Mouse      |
| Interact | Left Click |
| UI       | Mouse      |

Future Mobile Controls

- Virtual joystick
- Touch buttons
- Drag-and-drop interaction

---

# Camera System

The game contains two camera modes.

## Third Person

Used for:

- Exploration
- NPC interaction
- Navigation

## First Person

Used for:

- Desk
- Computer
- Filing cabinet
- Tax calculator
- Tax book
- Corkboard

The camera system is designed to be modular for future expansion.

---

# Finite State Machine (FSM)

## Game FSM

```
ReceiveCase

↓

ReviewDocuments

↓

InterviewClient

↓

ResearchTax

↓

ComputeTaxes

↓

AnalyzeEvidence

↓

StampAssessment

↓

PrepareReturn

↓

ComplianceAudit

↓

Rewards

↓

Outcome

↓

Archive

↓

CompleteCase
```

Each state is implemented as an independent class to maintain scalability and simplify future case additions.

---

## NPC FSM

```
Idle

↓

Waiting

↓

Interact

↓

Dialogue

↓

Completed
```

NPC behavior is also managed using a Finite State Machine, allowing new behaviors and dialogue states to be added without modifying existing logic.

---

# Save System

The prototype uses **JSON Serialization**.

Saved Data includes:

- Current Case
- Current State
- Objectives
- Dialogue Progress
- Player Position
- Case Data
- Statistics

The system is designed so it can later be replaced by cloud-based saves without major architectural changes.

---

# Coding Standards

The project follows clean architecture principles.

### Every script should include

- Purpose
- Responsibilities
- Inspector Variables
- Dependencies
- Public Methods
- Internal Logic
- Extensive Comments

General guidelines:

- Single Responsibility Principle
- Event-driven communication
- Interfaces where appropriate
- ScriptableObjects for reusable data
- Avoid singleton overuse
- Avoid tightly coupled systems
- Write scalable, maintainable code

---

# Current Progress

## Phase

🟢 Phase 1 — Greybox Prototype

Current milestone:

> **Milestone 1**
>
> - Unity project setup
> - Folder organization
> - Greybox office
> - Third-person movement
> - Placeholder assets

---

# Future Improvements

- Multiple tax cases
- Advanced dialogue system
- Voice acting
- Mobile touch controls
- Character animations
- Inventory system
- Achievement system
- Cloud save support
- Analytics
- Expanded NPC AI
- Additional taxation scenarios

---

# Contributors

Developed as a Capstone Project by:

- Ralph Jayrell Gacusan
- _(Add other members here)_

Institution:

**Technological Institute of the Philippines - Quezon City**

---

# License

This project is developed for educational and research purposes.

All rights reserved unless otherwise stated.

---

<div align="center">

**TaxVenture**

_"Learning Income Tax Through Interactive Gameplay."_

</div>
