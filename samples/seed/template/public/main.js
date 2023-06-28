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
  configureHljs: function (hljs) {
    hljs.registerLanguage('bicep', bicep);
  },
}
