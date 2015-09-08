# docfx default template
This template is based on [angularjs](https://angularjs.org/).

## Prerequisite
* [nodejs](https://nodejs.org/)
* [grunt](http://gruntjs.com/)

## Build & development
Run `npm install -g bower grunt-cli` to install bower and grunt globally.
Run `npm install` to install required nodejs components.
Run `bower install` to install required bower components.
Run `grunt` for building and `grunt serve` for preview.

## Testing
Running `grunt test` will run the unit tests with karma.

## Notice
As for this angular template, we use HTML5 mode other than hashbang, so you will get some kind of error if you refresh current page, such as 404 not found. 
The problem caused by the fact that angular's route strategy conflicts with back-end route strategy.
And the solution is to redirect the request to angular index in the back-end. 

There are two examples. 

For Express framework

```javascript
app.use('/one/path', function(req, res, next){
  var newPath = 'angular index page';
  res.set('Location', newPath)
     .status(301)
     .send();
})
```

For nginx, you need add `try_files` option in configuration file

```
server {
        set $docfx /www/deploy/mysite;
        listen 80;
        server_name site.me;
        location / {
            root $docfx;
            try_files $uri $uri/ /index.html =404;
        }
}
```