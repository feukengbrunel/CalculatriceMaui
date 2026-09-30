# Calculatrice .NET MAUI

Calculatrice mobile développée avec .NET MAUI (Single Project) dans le cadre de l'Activité n° 4 – Atelier de développement mobile.

## Fonctionnalités

**Opérations de base**
- Addition, soustraction, multiplication, division (enchaînement `2 + 3 × 4 =` → 20)
- Nombres décimaux (virgule), précision `decimal` : `0,1 + 0,2 = 0,3`
- `AC` remise à zéro totale, `⌫` effacement du dernier caractère
- `±` changement de signe, `%` pourcentage (`200 + 10 %` → 220, `50 × 10 %` → 5)
- Division par zéro gérée sans plantage (« Division par zéro impossible », `0 ÷ 0` → « Résultat indéfini »)
- Opération en cours affichée au-dessus du résultat

**Opérations avancées (bonus)**
- Racine carrée (entrée négative → « Entrée invalide »), carré, inverse `1/x`
- Historique défilant des 20 derniers calculs
- Répétition de `=` (`2 + 3 = =` → 8)
- Dépassement de capacité détecté, notation scientifique pour les grands nombres

**Interface**
- Disposition portrait (affichage en haut, clavier en bas) et paysage (affichage à gauche, clavier à droite)
- Touches et résultat redimensionnés selon la taille réelle de l'écran : aucun débordement sur petit écran
- Les nombres longs réduisent la taille de police au lieu de déborder

## Layouts utilisés (6 types sur la même page)

| Layout | Rôle |
|---|---|
| `Grid` | Grille racine qui répartit l'écran et se réorganise selon l'orientation, et grille 4 colonnes du clavier. |
| `Border` | Carte arrondie qui encadre la zone d'affichage. |
| `ScrollView` | Permet de faire défiler l'historique lorsqu'il dépasse la hauteur disponible. |
| `VerticalStackLayout` | Empile les lignes de l'historique (et l'expression / résultat de chaque ligne). |
| `HorizontalStackLayout` | Barre d'en-tête : titre, compteur d'historique et bouton « Vider ». |
| `FlexLayout` | Rangée des fonctions avancées dont la largeur se répartit automatiquement. |

## Structure

```
CalculatriceMaui/
├── MainPage.xaml(.cs)          Interface et gestionnaires d'événements
├── Services/CalculatorEngine.cs Logique de calcul, indépendante de l'UI
├── Resources/Styles/Calculator.xaml  Couleurs et styles des touches
└── Platforms/                  Code spécifique Android / iOS / Windows / Mac
```

## Lancer le projet

```bash
dotnet workload install maui
dotnet build -t:Run -f net10.0-android          # émulateur ou appareil Android
dotnet build -t:Run -f net10.0-windows10.0.19041.0  # Windows
```
