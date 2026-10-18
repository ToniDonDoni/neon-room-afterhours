const assert = require('node:assert/strict');
module.exports = async function assertEnglish(page) {
 const untranslated = await page.evaluate(() => {
   const values = [document.title, document.body.innerText];
   for (const el of document.querySelectorAll('[aria-label],[placeholder],[title],[alt],meta[name="description"]')) {
     for (const attr of ['aria-label','placeholder','title','alt','content']) if (el.hasAttribute(attr)) values.push(el.getAttribute(attr));
   }
   return values.filter(value => /\p{Script=Cyrillic}/u.test(value));
 });
 assert.deepEqual(untranslated, [], 'Visible copy and accessible labels must be English');
 assert.equal(await page.locator('html').getAttribute('lang'), 'en');
};
