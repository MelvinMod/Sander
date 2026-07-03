# Anti-Cheat & Performance Analysis - Sander Patch

## Overview
This document outlines potential areas for improvement in the Sander patch, focusing on bypassing anti-cheat mechanisms and optimizing FPS.

## Anti-Cheat Detection (Based on Example Patches)
*   **Mob State Monitoring:** The `mob_state.cpp` file (accessed previously) likely contains code that monitors player actions (movement, item usage, etc.) to detect modifications made by patches.
*   **CerberusWareV3 Approach:** CerberusWareV3 appears to utilize a module-based approach for anti-cheat, potentially employing techniques like memory scanning and signature analysis.

## Potential Bypass Strategies
*   **Obfuscation:** Implement code obfuscation techniques to make the patch's logic more difficult to understand and analyze by anti-cheat systems.
*   **Dynamic Code Loading:** Utilize dynamic code loading mechanisms to dynamically modify game behavior, making it harder for static analysis tools to detect changes.
*   **Process Injection Techniques:** Explore advanced process injection techniques to evade detection.

## Performance Optimization
*   **FPS+ Button:** Implement a button that allows users to manually increase FPS by adjusting rendering settings or utilizing performance-enhancing techniques (e.g., reducing graphical fidelity).
*   **Subtitles Smoothing:** Investigate methods for smoothing subtitles, potentially using interpolation techniques or adaptive frame rates.
*   **Mass Spectrometer Integration:** Integrate the mass spectrometer into the UI as requested, allowing users to access it via a button in the main menu.

## Next Steps
*   Further analysis of `CerberusWareV3/core/anti_cheat/detection_module.cpp` (if accessible).
*   Detailed investigation of mob state monitoring techniques.
