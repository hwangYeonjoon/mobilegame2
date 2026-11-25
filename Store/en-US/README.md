# English listing draft for 0.42.0

These files are local drafts, not submitted Play Console changes. Keep the currently published country targeting until the English build and regional service settings have been reviewed.

- `title.txt`: Scrap Guard
- `short-description.txt`: under 80 characters
- `full-description.txt`: 60 operations / 10 regions, robot assembly and piloting
- `release-notes-0.42.0.txt`: language support changes
- Take English store screenshots from the final release build. Development-build QA captures are evidence, not store artwork.

Google Play's purchase sheet uses product translations in Play Console and the customer's Play locale. The in-game language setting cannot replace those strings. Add these English translations to the existing products; keep product IDs, quantities, prices and purchase options unchanged:

| Product ID | English name | English description |
|---|---|---|
| gems_300 | 300 Gems | Receive 300 Gems to use in Scrap Guard. |
| gems_600 | 600 Gems | Receive 600 Gems to use in Scrap Guard. |
| gems_1200 | 1,200 Gems | Receive 1,200 Gems to use in Scrap Guard. |
| gems_2600 | 2,600 Gems | Receive 2,600 Gems to use in Scrap Guard. |
| ad_skip_lifetime | Lifetime Ad Skip | Skip eligible rewarded ads. Existing reward limits still apply. |

Before adding countries: publish the English app/listing/product translations, provide an English privacy policy and account-deletion page, and check the intended countries' product availability and consent flow. This task does not alter the production server, Play country targeting, prices or Google review state.
