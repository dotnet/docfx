import { bicep } from './bicep.js'

export default {
  iconLinks: [
    {
      icon: 'github',
      href: 'https://github.com/dotnet/docfx',
      title: 'GitHub'
    },
    {
      icon: 'twitter',
      href: 'https://twitter.com/',
      title: 'Twitter'
    }
  ],
  lunrLanguages: ['en', 'ru'],
  start() {
    console.log('started');
  },
  configureHljs (hljs) {
    hljs.registerLanguage('bicep', bicep);
  },
}

