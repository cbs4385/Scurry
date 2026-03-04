# Changelog

All notable changes to Scurry: Tales of the Rat Pack will be documented in this file.

## [0.1.0] - 2026-03-04

### Added
- 4x4 tile grid board with four tile types: Normal, Resource Node, Enemy Patrol, Hazard
- 10 placeholder cards: 5 hero rats (Scout, Brawler, Pack, Nimble, Guard) and 5 resource cards (Food Scrap, Crumb Pile, Cardboard Shelter, Bottle Cap Armor, Shiny Coin)
- Card draw and hand display (5 cards per turn) with shuffle and discard cycling
- Drag-and-drop card placement: heroes on Normal/Resource Node tiles, resources on Normal tiles
- Game phase state machine: Draw, Deploy, Gather, Resolve
- Automated gathering phase: heroes pathfind toward nearest resource via A*
- Combat resolution: hero combat vs enemy strength, colony HP damage on loss
- Colony HP system (starting 20, max 50) with healing from food collection
- Game over detection when colony HP reaches 0
- HUD with phase label, colony HP bar, and End Turn button
- Tile legend panel showing tile type colors and descriptions
- Hover tooltip on tiles displaying type, stats, and current status
- Comprehensive diagnostic logging across all scripts
