
## StubHttpServer

This project provides a quick and easy way to emulate external http APIs and test your .Net HTTP Clients. 

## Examples

**Expecting OK Status**

    [Test]
    public async Task ShouldReturnTwoRestaurants(){
    
        var apiResponse = new SearchByOutcode(){
	    Restaurants = new List<Restaurant>
		    {
			    new Restaurant{Name = "Pizza",CuisineTypes = new List<CuisineType>()},
			    new Restaurant(){Name = "Burger",CuisineTypes = new List<CuisineType>()}
		    }
	    };
	    
	    using (var server = new StubHttpServer.StubHttpServer())
	    {
    		server.SetupRoute("/restaurants?q=xx11")
    		.Get()
    		.ReturnsStatusCode(HttpStatusCode.OK)
    		.WithJsonContent(wpiResponse);
    
    		var client = new My.ApiClient.ApiClient(server.Url, "xxx");
    		var result = await client.SearchRestaurantsByOutcode("xx11");
    
    		result.Should().NotBeNull();
    		result.Restaurants.Count.Should().Be(2);
    		result.Restaurants[0].Name.Should().Be("Pizza");
    		result.Restaurants[1].Name.Should().Be("Burger");
	    }
    }



**Expecting other statuses**
   
    [Test]
    public async Task ShouldThrowApiexceptionWhenServerRepliesWithANotOKStatus()
    {
        using (var server = new StubHttpServer.StubHttpServer())
        {
            server.SetupRoute("/restaurants?q=xx11")
                .Get()
                .ReturnsStatusCode(HttpStatusCode.InternalServerError)
                .WithNoContent();
            try
            {
                var client = new My.ApiClient.ApiClient(server.Url, "xxx");
                var result = await client.SearchRestaurantsByOutcode("xx11");
            }
            catch (ApiException ex)
            {
                ex.Code.Should().Be(HttpStatusCode.InternalServerError);
                return;
            }
            catch (Exception ex)
            {
                Assert.Fail();
            }
 
            Assert.Fail();
        }
    }



**Timeout**

    [Test]
    public async Task ShouldThrowExceptionWhenRequestTimeouts()
    {
        using (var server = new StubHttpServer.StubHttpServer())
        {
            server.SetupRoute("/restaurants?q=xx11")
                .Get()
                .HangsFor(new TimeSpan(0,0,2))
                .ThenReturnsStatusCode(HttpStatusCode.OK)
                .WithNoContent();

            try
            {
                var client = new My.ApiClient.ApiClient(server.Url, "xxx",1);
                var result = await client.SearchRestaurantsByOutcode("xx11");
            }
            catch (ApiException ex)
            {
                ex.Message.Should().Contain("The operation was cancelled");
                return;
            }
            catch (Exception ex)
            {
                Assert.Fail();
            }
            Assert.Fail();
        }
    }
