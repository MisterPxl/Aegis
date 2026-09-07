**Corrections de l’audit Aegis — 7 septembre 2026**

Les huit points de l’audit ont été corrigés. La validation finale sous Unity 6000.4.0f1 réussit : **33 tests EditMode sur 33**, dont 29 pour le cœur et 4 pour l’intégration Valkyrie. Le sample conserve son type après réimport et la compilation des scripts Player avec le sample importé produit trois assemblies sans erreur.

| Point | Correction |
| --- | --- |
| Références cycliques | Parcours commun `AegisSerializedProperties`, avec suivi des identifiants de références managées déjà visités. Le cœur et Valkyrie partagent cette protection. |
| Persistance des règles | Six règles intégrées et deux règles Valkyrie dans des fichiers correspondant à leurs classes, avec métadonnées Unity stables. Tests de sauvegarde/réimport et de conservation de la configuration. |
| Sample | Script déplacé dans `Editor/SceneNamingRule.cs`, exclu de la compilation Player. |
| Champs masqués | Parcours de toutes les propriétés sérialisées, y compris les références et collections sous `HideInInspector`. |
| Suppressions | Finalisation commune des findings et compteurs, appliquée aux runs synchrones/interactifs, au dashboard et au rerun d’une règle. Sélection supprimée invalidée, rapport mis à jour après suppression. |
| JUnit | Échecs déterminés par les findings actifs et le seuil du profil. Exceptions en `error`, règles ignorées en `skipped`, compteurs cohérents. Le CLI exporte également les rapports en cas d’exception de règle et retourne le code 4. |
| Profils | Initialisation des trois profils avant sauvegarde et migration des anciennes valeurs Build/CI erronées. Conservation des filtres, seuils, règles désactivées et budgets personnalisés non affectés. |
| Annulation | Résultat unsuccessful marqué `IsCancelled`, résumé explicitement incomplet, état conservé en JSON et signalé en JUnit. Le dernier rapport terminé reste intact. |

Les scénarios CLI ont été exécutés dans de vrais processus Unity. Le build gate a également été appelé sur les mêmes fixtures.

| Scénario | Code CLI | Build gate | JUnit |
| --- | --- | --- | --- |
| Warning, seuil Error | 0 | Autorise | 0 failure, 0 error |
| Warning, seuil Warning | 2 | Bloque | 1 failure, 0 error |
| Diagnostic supprimé | 0 | Autorise | 0 failure, 0 error |
| Exception de règle | 4 | Bloque | 0 failure, 1 error |
| Profil CLI inconnu | 3 | Sans objet pour ce contrôle CLI | Pas d’export |

Preuves : [tests EditMode](/Users/alexis.gomes/Projects/Aegis/Documentation~/Audit-2026-09-07/fixed-tests.xml), [CLI et build gate](/Users/alexis.gomes/Projects/Aegis/Documentation~/Audit-2026-09-07/fixed-cli-validation.json), [compilation Player](/Users/alexis.gomes/Projects/Aegis/Documentation~/Audit-2026-09-07/fixed-player-compilation.txt), [sample et profils](/Users/alexis.gomes/Projects/Aegis/Documentation~/Audit-2026-09-07/fixed-sample-and-profiles.txt).

Les tests de régression se trouvent dans [AegisRegressionTests.cs](/Users/alexis.gomes/Projects/Aegis/Tests/EditMode/AegisRegressionTests.cs) et [AegisValkyrieRegressionTests.cs](/Users/alexis.gomes/Projects/Aegis/Addons~/Valkyrie/Tests/EditMode/AegisValkyrieRegressionTests.cs). Les métadonnées Unity ont été contrôlées : chaque nouveau script possède son `.meta`, sans GUID dupliqué. `git diff --check` passe.

La validation a utilisé une copie isolée du projet, les packages core/addon dans des dossiers distincts, et Valkyrie 1.5.0. Un premier montage local avec l’addon physiquement imbriqué dans le package parent empêchait Unity d’associer ses scripts à leur assembly ; les tests passent avec des packages séparés, comme lors d’une installation Git. Cette contrainte de développement local est documentée dans le README de l’extension.

Les anciens assets déjà privés de leur référence de script ne sont pas réécrits automatiquement : ils doivent être réparés ou recréés. Lors de la mise à jour d’un sample déjà importé, retirer l’ancien `AegisCustomRuleExample.cs` pour éviter une définition de classe en double. Les règles restent synchrones sur le thread de l’éditeur ; le budget de frame ne préempte pas une règle en cours. Unity 6000.0, le runtime Helios et un build Player complet n’ont pas été exécutés dans cette validation.
