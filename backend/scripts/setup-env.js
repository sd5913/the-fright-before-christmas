import { randomBytes } from 'node:crypto';
import { readFile, writeFile } from 'node:fs/promises';

const template = await readFile(new URL('../.env.example', import.meta.url), 'utf8');
const contents = template.replaceAll('replace-with-a-random-alphanumeric-password', randomBytes(32).toString('hex'));
try {
  await writeFile(new URL('../.env', import.meta.url), contents, { flag: 'wx', mode: 0o600 });
  console.log('Created backend/.env with a random database password. No secret was printed.');
} catch (error) {
  if (error.code === 'EEXIST') console.log('backend/.env already exists; left it unchanged.');
  else throw error;
}
