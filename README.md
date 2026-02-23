# NPC-with-Adaptive-Difficulty

Σύντομο πρωτότυπο σε **Unity* για διπλωματική εργασία, με στόχο την υλοποίηση και μελέτη **NPC συμπεριφοράς με προσαρμοστική δυσκολία (Adaptive Difficulty / DDA)** σε FPS σενάριο.

## Τι περιλαμβάνει

* **FPS gameplay demo** σε ελεγχόμενη arena (waves)
* **NPC agent** με συμπεριφορά βασισμένη σε **Unity ML-Agents**
* **Υβριδικό σύστημα δυσκολίας**:

  * learned behavior μέσω RL policy (conditioned by difficulty)
  * hardcoded difficulty knobs (π.χ. aim error, cooldown, move speed)
* **Telemetry / Logging / HUD** για παρατήρηση μετρικών παίκτη και εχθρού
* **ONNX / NNModel inference** για χρήση του εκπαιδευμένου μοντέλου στο gameplay

## Στόχος έργου

Η εργασία εξετάζει πώς μπορεί να συνδυαστεί η **Ενισχυτική Μάθηση (RL)** με έναν πρακτικό μηχανισμό **Dynamic Difficulty Adjustment (DDA)**, ώστε το ίδιο μοντέλο NPC να προσαρμόζει τη συμπεριφορά του σε διαφορετικά επίπεδα δυσκολίας.

## Σημείωση

Το project αποτελεί **ερευνητικό / ακαδημαϊκό πρωτότυπο (proof-of-concept)** και όχι production-ready παιχνίδι.

---------------------------------------------------------------------------------------------------------------------


A **Unity** prototype project (thesis-related) focused on implementing and studying **adaptive-difficulty NPC behavior (Adaptive Difficulty / DDA)** in an FPS scenario.

## Features

* **FPS gameplay demo** in a controlled arena (wave-based)
* **NPC agent** behavior powered by **Unity ML-Agents (PPO)**
* **Hybrid difficulty system**:

  * learned behavior via RL policy (difficulty-conditioned)
  * hardcoded difficulty knobs (e.g., aim error, cooldown, move speed)
* **Telemetry / Logging / HUD** for observing player and NPC metrics
* **ONNX / NNModel inference** for using the trained model in gameplay

## Project Goal

This project explores how **Reinforcement Learning (RL)** can be combined with a practical **Dynamic Difficulty Adjustment (DDA)** mechanism, allowing a single NPC model to adapt its behavior across different difficulty levels.

## Note

This repository is a **research / academic proof-of-concept**, not a production-ready game.

