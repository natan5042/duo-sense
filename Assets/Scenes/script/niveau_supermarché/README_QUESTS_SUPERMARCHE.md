# Quêtes niveau supermarché — réglage

## 1. Panneau de quêtes (lié à la caméra)

**Option A — Outil éditeur**  
Menu Unity : **Tools > Supermarché > Créer panneau de quêtes (lié à la caméra)**.  
Cela crée un Canvas en **Screen Space - Camera** (donc fixé à l’écran, dessiné par la caméra).

**Option B — À la main**  
1. Créer un GameObject vide, y ajouter **Canvas**.  
2. Canvas : **Render Mode = Screen Space - Camera**, **Render Camera = Main Camera**.  
3. Sous le Canvas : créer une **Image** (panneau) et un **Text** (texte des quêtes).  
4. Sur le Canvas (ou sur un enfant), ajouter le script **QuestUI** et assigner le **Quest Text** (et éventuellement la **Quest Panel**).

**Pour déplacer/redimensionner**  
Sélectionner le **QuestPanel** dans la hiérarchie, puis modifier le **RectTransform** (Position X/Y, Width/Height). Le Canvas reste attaché à la caméra grâce au mode Screen Space - Camera.

---

## 2. Quête liste (à l’ouverture du panneau)

**Option A — Automatique**  
Sur le même objet que **LevelDisplayInteraction** (celui qui ouvre le panneau liste), ajoute le script **ShoppingListQuestTrigger** et assigne **List Canvas** = le Canvas du panneau liste (souvent le même que Menu Canvas). Dès que ce Canvas s’ouvre, la quête « Acheter les produits » est ajoutée (Lait x2, Soupe, Croquette, Eau x3, Spaghetti).

**Option B — Événement**  
Sur **LevelDisplayInteraction**, dans **On Menu Opened**, ajoute un listener : GameObject avec **SupermarketQuestManager** → **EnsureShoppingQuestAdded**.

---

## 3. SupermarketQuestManager

Dans la scène **quete_croquettes** :

1. Créer un GameObject vide, nommer par ex. `SupermarketQuestManager`.
2. Ajouter le script **SupermarketQuestManager**.
3. **Board Item Id** : `planche` (doit correspondre au **PickableItem** de la planche).
4. **Ramp Item Id** : `trampoline` (id du trampoline).
5. **Shopping List Item Id** : `liste_courses` (id de l’objet « liste de course »).
6. **Shopping List** : laisser vide pour utiliser la liste par défaut (Lait x2, Soupe, Croquette, Eau x3, Spaghetti), ou remplir avec les **itemId** des produits de ta scène.
7. **Wheelchair Respawn Point** : un Transform (vide) placé où le fauteuil doit réapparaître s’il tombe dans la fosse.

Sur les objets de la scène :

- **Planche** : composant **PickableItem**, **Item Id** = `planche`.
- **Trampoline(s)** : **PickableItem**, **Item Id** = `trampoline`.
- **Liste de course** (feuille à ramasser) : **PickableItem**, **Item Id** = `liste_courses`.
- **Produits** (lait, soupe, croquette, eau, spaghetti) : **PickableItem** avec **Item Id** = `lait`, `soupe`, `croquette`, `eau`, `spaghetti` (en minuscules, comme dans la liste par défaut).

---

## 4. Zone caisse (CheckoutZone)

1. À l’emplacement de la caisse, créer un GameObject avec un **BoxCollider2D** en **Trigger**.
2. Ajouter le script **CheckoutZone**.
3. Quand le joueur entre dans la zone **avec un produit en main**, le produit est considéré comme scanné : le compteur de la quête se met à jour et l’objet est retiré.

---

## 5. Fosse (PitRespawnZone)

1. Créer un GameObject dans la **fosse** (le trou où le fauteuil peut tomber).
2. Ajouter un **BoxCollider2D** en **Trigger** qui couvre la fosse.
3. Ajouter le script **PitRespawnZone**.
4. **Respawn Point** : assigner le même Transform que **Wheelchair Respawn Point** du SupermarketQuestManager (ou laisser vide pour utiliser celui du manager).

Résultat : si le joueur prend seulement le trampoline et saute dans la fosse, il est téléporté au point de respawn au lieu de rester bloqué.

---

## 6. QuestSystem

Une scène doit contenir un GameObject avec le script **QuestSystem** (souvent en DontDestroyOnLoad depuis le hub ou la maison). Si la scène supermarché n’en a pas, en ajouter un (vide + script **QuestSystem**).
