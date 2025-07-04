# ItemInfoDisplay

Displays item information to the left of item prompts (which is most likely bottom right of your screen).
It just inefficiently scans all the components of an item to pull information from.

Displays:
- Status Changes (e.g. Hunger, Poison)
- Weight
- Remaining Item Uses
- Cookability
- Number of Times Cooked
- Some Effects & Afflictions

This mod is mainly for getting the numbers on consumable items.
It has limited quantification of equipment effects such as ropes.

Configurable:
- Font Size
- Text Outline Width
- Size Delta X (suboptimal workaround to move the text horizontally)
- Force Update Time

Overall, the mod's pretty jank, but it kinda works (for now).
It'll probably break after some update since devs are still actively working on things.

I still don't understand how remedy fungus calculations actually work. Why is a -0.2 status change causing a -0.175 actual status change? Why is a -0.015 status change every 0.5 seconds for 11 seconds causing -0.275 status change instead of -0.333? I know I'm pulling data from the right place, because if I change it, the results also change so... I frankensteined some nonsense to get close enough to the result.

Also, here's some interesting bugs or stuff I found in 1.5.a:
- You can cook food 12x to add up to 90 Poison, but if you unequip & reequip, it resets to 10 Poison
- Multiple use items (antidote, cure-all, scout cookies) don't apply some added cooking benefits until you use the final item charge
- Cooking benefits are added to non-consumable items that aren't actually usable (I hid most of these)
- Some items (e.g. blow dart, scout effigy) apply the cooking benefits to you
